using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MotelLease.Api.Authentication;
using MotelLease.Application.Common.Abstractions;
using MotelLease.Application.Common.Errors;
using MotelLease.Application.Common.Security;
using MotelLease.Domain.Enums;
using MotelLease.Infrastructure.Security;

namespace MotelLease.Api.Extensions;

public static class AuthenticationSetup
{
    /// <summary>
    /// Layer one of authorization: what role the caller has, read from the access token.
    /// Whether that caller may touch a particular boarding house is a separate, data-driven
    /// check (docs/domain-rules.md §6) and deliberately not expressed as a policy here.
    /// </summary>
    public static IServiceCollection AddApiAuthentication(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddScoped<IRequestContext, RequestContext>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

        // Configured through IConfigureOptions rather than a lambda so JwtOptions arrives from
        // the container — the same instance the token service signs with.
        services.AddSingleton<IConfigureOptions<JwtBearerOptions>, ConfigureJwtBearerOptions>();

        services.AddAuthorizationBuilder()
            // Authentication is the default; an endpoint opens itself up with [AllowAnonymous].
            // Forgetting the attribute then fails closed instead of exposing the endpoint.
            .SetFallbackPolicy(new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build())
            .AddPolicy(AuthPolicies.RequireTenant, policy =>
                policy.RequireRole(nameof(UserRole.Tenant)))
            .AddPolicy(AuthPolicies.RequireOwner, policy =>
                policy.RequireRole(nameof(UserRole.Owner)))
            // Staff act on behalf of an owner, so an owner is always allowed where staff are.
            .AddPolicy(AuthPolicies.RequireStaffOrOwner, policy =>
                policy.RequireRole(nameof(UserRole.Staff), nameof(UserRole.Owner)))
            .AddPolicy(AuthPolicies.RequireAdmin, policy =>
                policy.RequireRole(nameof(UserRole.Admin)));

        return services;
    }

    internal static async Task WriteProblemAsync(
        HttpContext context,
        int status,
        string title,
        string messageKey)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        var localizer = context.RequestServices.GetRequiredService<ILocalizer>();
        var requestContext = context.RequestServices.GetRequiredService<IRequestContext>();

        context.Response.StatusCode = status;

        await context.Response.WriteAsJsonAsync(
            new
            {
                status,
                title,
                detail = localizer.Get(messageKey, requestContext.Language),
                instance = context.Request.Path.Value,
                code = messageKey,
                traceId = context.TraceIdentifier
            },
            options: (JsonSerializerOptions?)null,
            contentType: "application/problem+json",
            context.RequestAborted);
    }
}

internal sealed class ConfigureJwtBearerOptions(IOptions<JwtOptions> jwtOptions)
    : IConfigureNamedOptions<JwtBearerOptions>
{
    public void Configure(string? name, JwtBearerOptions options)
    {
        if (name != JwtBearerDefaults.AuthenticationScheme)
        {
            return;
        }

        Configure(options);
    }

    public void Configure(JwtBearerOptions options)
    {
        var jwt = jwtOptions.Value;

        // Claims are used exactly as issued; the default mapping would rewrite "sub" into a
        // long WS-Federation URI and CurrentUser would stop finding it.
        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
            ValidateLifetime = true,
            // A 15-minute token does not need slack for clock drift; the refresh endpoint is
            // the intended way to get a fresh one.
            ClockSkew = TimeSpan.Zero,
            NameClaimType = JwtRegisteredClaimNames.Sub,
            RoleClaimType = JwtRegisteredClaimNames.Role
        };

        options.Events = new JwtBearerEvents
        {
            // Without this a rejected token yields an empty 401 body; every other failure in
            // the API is problem+json, so this one is too.
            OnChallenge = async context =>
            {
                context.HandleResponse();

                await AuthenticationSetup.WriteProblemAsync(
                    context.HttpContext,
                    StatusCodes.Status401Unauthorized,
                    "Unauthorized",
                    MessageKeys.General.Unauthorized);
            },
            OnForbidden = context => AuthenticationSetup.WriteProblemAsync(
                context.HttpContext,
                StatusCodes.Status403Forbidden,
                "Forbidden",
                MessageKeys.General.Forbidden)
        };
    }
}
