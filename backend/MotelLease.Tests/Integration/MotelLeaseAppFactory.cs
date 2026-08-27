using System.Collections.Concurrent;
using System.Globalization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MotelLease.Application.Common.Abstractions;
using MotelLease.Infrastructure.Persistence;

namespace MotelLease.Tests.Integration;

/// <summary>
/// Boots the real API against the container database. Only two things are substituted: the
/// connection string and the email sender, because the OTP a test needs to read only exists
/// inside a message.
/// </summary>
/// <param name="authPermitLimit">
/// Requests allowed per window on the authentication policy. Every request in a run arrives from
/// the same (absent) address and therefore shares one partition, so the default is wide enough
/// that flow tests never trip it. <see cref="RateLimitTests"/> passes a small value instead.
/// </param>
/// <param name="otpPermitLimit">The same, for the OTP policy.</param>
/// <param name="googleClientId">
/// Pinned as the expected audience of a Google ID token. Left null on purpose by the tests that
/// assert an unconfigured client id makes the endpoint refuse rather than accept anything.
/// </param>
/// <param name="googleTokens">
/// Substitutes Google's own verification so the handler branches after it — unverified address,
/// account linking, a locked account — can be reached. Null keeps the real verifier, which is what
/// the tests about malformed tokens need.
/// </param>
public sealed class MotelLeaseAppFactory(
    string connectionString,
    int authPermitLimit = 10_000,
    int otpPermitLimit = 10_000,
    string? googleClientId = null,
    IGoogleTokenVerifier? googleTokens = null) : WebApplicationFactory<Program>
{
    public RecordingEmailSender Emails { get; } = new();

    /// <summary>
    /// Log entries the app produced. Some guards are only observable here: refusing Google
    /// sign-in because no client id is configured returns the same 401 as a malformed token, so
    /// the log is what distinguishes the two.
    /// </summary>
    public RecordingLoggerProvider Logs { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.UseSetting("ConnectionStrings:Default", connectionString);
        // Deterministic and long enough for the HS256 key check; never used outside tests.
        builder.UseSetting("Jwt:SigningKey", "integration-test-signing-key-0123456789abcdef");
        builder.UseSetting("Jwt:Issuer", "motellease-tests");
        builder.UseSetting("Jwt:Audience", "motellease-tests");

        builder.UseSetting(
            "RateLimiting:Authentication:PermitLimit",
            authPermitLimit.ToString(CultureInfo.InvariantCulture));
        builder.UseSetting(
            "RateLimiting:Otp:PermitLimit",
            otpPermitLimit.ToString(CultureInfo.InvariantCulture));

        // The API project has a UserSecretsId, and Development loads it. Without blanking these
        // the suite would inherit whatever the developer has configured — real Cloudinary
        // credentials, a real Google client id — so a test would behave differently on a laptop
        // than in CI, and an upload test would write to the live account. Every externally
        // configured value is therefore set explicitly, empty unless a test asked for it.
        builder.UseSetting("GoogleAuth:ClientId", googleClientId ?? string.Empty);
        builder.UseSetting("Smtp:Host", string.Empty);
        builder.UseSetting("Cloudinary:CloudName", string.Empty);
        builder.UseSetting("Cloudinary:ApiKey", string.Empty);
        builder.UseSetting("Cloudinary:ApiSecret", string.Empty);

        builder.ConfigureServices(services =>
        {
            services.AddSingleton<ILoggerProvider>(Logs);

            services.RemoveAll<IEmailSender>();
            services.AddSingleton<IEmailSender>(Emails);

            if (googleTokens is not null)
            {
                services.RemoveAll<IGoogleTokenVerifier>();
                services.AddSingleton(googleTokens);
            }
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

/// <summary>
/// Stands in for Google. A real ID token is signed by Google and cannot be minted in a test, so
/// the branches the handler runs *after* verification are reached by substituting the answer.
/// </summary>
public sealed class StubGoogleTokenVerifier(GoogleIdentity? identity) : IGoogleTokenVerifier
{
    /// <summary>Set when the token is meant to look valid but the address is not confirmed.</summary>
    public static GoogleIdentity Unverified(string email) =>
        new($"google-{Guid.NewGuid():N}", email, EmailVerified: false, "Someone", null);

    public static GoogleIdentity Verified(string email, string? subject = null) =>
        new(subject ?? $"google-{Guid.NewGuid():N}", email, EmailVerified: true, "Someone", null);

    public Task<GoogleIdentity?> VerifyAsync(
        string idToken,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(identity);
}

/// <summary>Keeps every log entry the app writes, so a test can assert on one.</summary>
public sealed class RecordingLoggerProvider : ILoggerProvider
{
    private readonly ConcurrentQueue<string> _entries = new();

    public ILogger CreateLogger(string categoryName) => new Recorder(_entries);

    public void Dispose() { }

    public bool Any(LogLevel level, string containing) =>
        _entries.Any(e =>
            e.StartsWith(level.ToString(), StringComparison.Ordinal)
            && e.Contains(containing, StringComparison.Ordinal));

    public string Dump() => _entries.Count == 0 ? "<no entries>" : string.Join("\n", _entries);

    private sealed class Recorder(ConcurrentQueue<string> entries) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) =>
            entries.Enqueue($"{logLevel}: {formatter(state, exception)}");
    }
}
