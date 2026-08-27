using System.Collections.Concurrent;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using MotelLease.Application.Common.Abstractions;
using MotelLease.Infrastructure.Persistence;

namespace MotelLease.Tests.Integration;

/// <summary>
/// Boots the real API against the container database. Only two things are substituted: the
/// connection string and the email sender, because the OTP a test needs to read only exists
/// inside a message.
/// </summary>
public sealed class MotelLeaseAppFactory(string connectionString) : WebApplicationFactory<Program>
{
    public RecordingEmailSender Emails { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.UseSetting("ConnectionStrings:Default", connectionString);
        // Deterministic and long enough for the HS256 key check; never used outside tests.
        builder.UseSetting("Jwt:SigningKey", "integration-test-signing-key-0123456789abcdef");
        builder.UseSetting("Jwt:Issuer", "motellease-tests");
        builder.UseSetting("Jwt:Audience", "motellease-tests");

        // Every request in the suite arrives from the same (absent) address, so the shared
        // per-IP window would reject later tests. The limiter has its own test.
        builder.UseSetting("RateLimiting:Authentication:PermitLimit", "10000");
        builder.UseSetting("RateLimiting:Otp:PermitLimit", "10000");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IEmailSender>();
            services.AddSingleton<IEmailSender>(Emails);
        });
    }

    /// <summary>
    /// Applies migrations rather than EnsureCreated: the generated column and the partial
    /// indexes live in the migrations, and a test must run against the shipped schema.
    /// </summary>
    public async Task MigrateAsync()
    {
        using var scope = Services.CreateScope();

        await scope.ServiceProvider
            .GetRequiredService<MotelLeaseDbContext>()
            .Database
            .MigrateAsync();
    }
}

public sealed class RecordingEmailSender : IEmailSender
{
    private readonly ConcurrentQueue<EmailMessage> _sent = new();

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        _sent.Enqueue(message);

        return Task.CompletedTask;
    }

    /// <summary>The six digits from the most recent message to this address.</summary>
    public string LastCodeFor(string email)
    {
        var message = _sent.LastOrDefault(m =>
            string.Equals(m.ToEmail, email, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"No email was sent to {email}.");

        var match = System.Text.RegularExpressions.Regex.Match(message.HtmlBody, @"\d{6}");

        return match.Success
            ? match.Value
            : throw new InvalidOperationException($"No code in the email to {email}.");
    }

    public bool AnySentTo(string email) =>
        _sent.Any(m => string.Equals(m.ToEmail, email, StringComparison.OrdinalIgnoreCase));
}
