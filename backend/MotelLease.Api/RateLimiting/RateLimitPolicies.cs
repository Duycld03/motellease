using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using MotelLease.Application.Common.Abstractions;
using MotelLease.Application.Common.Errors;

namespace MotelLease.Api.RateLimiting;

public static class RateLimitPolicies
{
    /// <summary>Login, register, Google sign-in, password reset — credential guessing.</summary>
    public const string Authentication = "authentication";

    /// <summary>Anything that sends an email, so the mailbox cannot be used as a weapon.</summary>
    public const string Otp = "otp";
}

/// <summary>
/// Limits per policy, overridable from configuration so a test run or a load test can widen
/// them without touching code.
/// </summary>
public sealed class RateLimitOptions
{
    public const string SectionName = "RateLimiting";

    public WindowLimit Authentication { get; set; } = new() { PermitLimit = 10, WindowSeconds = 60 };

    public WindowLimit Otp { get; set; } = new() { PermitLimit = 5, WindowSeconds = 300 };

    public sealed class WindowLimit
    {
        public int PermitLimit { get; set; }

        public int WindowSeconds { get; set; }
    }
}

public static class RateLimitingSetup
{
    /// <summary>
    /// Partitioned by remote address. The OTP service already throttles per email address;
    /// this is the coarser net that stops one caller from cycling through addresses.
    /// </summary>
    public static IServiceCollection AddApiRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var limits = configuration.GetSection(RateLimitOptions.SectionName).Get<RateLimitOptions>()
            ?? new RateLimitOptions();

        return services.AddRateLimiter(options =>
        {
            options.AddPolicy(
                RateLimitPolicies.Authentication,
                PartitionByCaller(limits.Authentication));

            options.AddPolicy(RateLimitPolicies.Otp, PartitionByCaller(limits.Otp));

            options.OnRejected = async (context, cancellationToken) =>
            {
                var retryAfter = context.Lease.TryGetMetadata(
                    MetadataName.RetryAfter,
                    out var window)
                    ? window
                    : TimeSpan.FromMinutes(1);

                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.HttpContext.Response.Headers.RetryAfter =
                    ((int)Math.Ceiling(retryAfter.TotalSeconds))
                    .ToString(CultureInfo.InvariantCulture);

                var services = context.HttpContext.RequestServices;
                var localizer = services.GetRequiredService<ILocalizer>();
                var requestContext = services.GetRequiredService<IRequestContext>();

                // Written by hand rather than thrown: the rate limiter sits in front of the
                // exception middleware, so nothing downstream would catch it.
                await context.HttpContext.Response.WriteAsJsonAsync(
                    new
                    {
                        status = StatusCodes.Status429TooManyRequests,
                        title = "Too Many Requests",
                        detail = localizer.Get(
                            MessageKeys.General.RateLimited,
                            requestContext.Language),
                        code = MessageKeys.General.RateLimited,
                        traceId = context.HttpContext.TraceIdentifier
                    },
                    options: null,
                    contentType: "application/problem+json",
                    cancellationToken);
            };
        });
    }

    private static Func<HttpContext, RateLimitPartition<string>> PartitionByCaller(
        RateLimitOptions.WindowLimit limit) =>
        context => RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = limit.PermitLimit,
                Window = TimeSpan.FromSeconds(limit.WindowSeconds),
                QueueLimit = 0
            });
}
