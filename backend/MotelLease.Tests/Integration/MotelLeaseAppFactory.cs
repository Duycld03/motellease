using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MotelLease.Api.Jobs;
using MotelLease.Application.Common.Abstractions;
using MotelLease.Application.Notifications;
using MotelLease.Application.Notifications.Contracts;
using MotelLease.Infrastructure.Payments;
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
/// <param name="imageStorage">
/// Substitutes Cloudinary. No credentials are configured in a test run, so the real registration
/// is <c>UnconfiguredImageStorage</c>, which throws; the image tests pass
/// <see cref="RecordingImageStorage"/> instead of uploading to a live account.
/// </param>
public sealed class MotelLeaseAppFactory(
    string connectionString,
    int authPermitLimit = 10_000,
    int otpPermitLimit = 10_000,
    string? googleClientId = null,
    IGoogleTokenVerifier? googleTokens = null,
    IImageStorage? imageStorage = null) : WebApplicationFactory<Program>
{
    /// <summary>The timed sweeps, unregistered so a tick cannot land inside a test.</summary>
    private static readonly Type[] ScheduledJobs =
        [typeof(AppointmentExpiryJob), typeof(DepositExpiryJob)];

    public RecordingEmailSender Emails { get; } = new();

    /// <summary>
    /// Stands in for the SignalR hub. A real WebSocket client would only prove that the transport
    /// works; what the tests are about is which recipient gets told what, and in whose language.
    /// </summary>
    public RecordingNotificationRealtime Realtime { get; } = new();

    /// <summary>
    /// Log entries the app produced. Some guards are only observable here: refusing Google
    /// sign-in because no client id is configured returns the same 401 as a malformed token, so
    /// the log is what distinguishes the two.
    /// </summary>
    public RecordingLoggerProvider Logs { get; } = new();

    /// <summary>
    /// Stands in for MoMo's create-payment endpoint, which is a real HTTP call to MoMo and the one
    /// part of that gateway a test cannot make. Keeps the request body so a test can assert what was
    /// sent, and what was signed.
    /// </summary>
    public RecordingMoMoApi MoMoApi { get; } = new();

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

        // The real VNPay gateway runs against a fixed test secret rather than being substituted: the
        // signature is the whole authentication story of an IPN callback, so a test that stubbed it
        // out would assert nothing about the rule it is there to prove (docs/domain-rules.md §9.8).
        // The tests sign their own callbacks with the same secret, independently of this code.
        builder.UseSetting("VnPay:TmnCode", VnPayTestMerchant.TmnCode);
        builder.UseSetting("VnPay:HashSecret", VnPayTestMerchant.HashSecret);
        builder.UseSetting("App:ApiBaseUrl", "http://localhost");
        builder.UseSetting("App:WebBaseUrl", "http://localhost:3000");

        // MoMo, on the same terms. Its create call is the one part that cannot run for real in a
        // test — it is an HTTP request to MoMo — so only that call is substituted; the signing and
        // the callback reading are the shipped code.
        builder.UseSetting("MoMo:PartnerCode", MoMoTestMerchant.PartnerCode);
        builder.UseSetting("MoMo:AccessKey", MoMoTestMerchant.AccessKey);
        builder.UseSetting("MoMo:SecretKey", MoMoTestMerchant.SecretKey);

        builder.ConfigureServices(services =>
        {
            services.AddSingleton<ILoggerProvider>(Logs);

            // The background sweeps run on their own clock and rewrite rows. Left registered,
            // a test's outcome would depend on whether a tick happened to land inside it, so
            // the schedule is dropped and the rule behind it is invoked directly instead.
            foreach (var descriptor in services
                         .Where(d => d.ServiceType == typeof(IHostedService)
                                     && d.ImplementationType is not null
                                     && ScheduledJobs.Contains(d.ImplementationType))
                         .ToList())
            {
                services.Remove(descriptor);
            }

            services.RemoveAll<IEmailSender>();
            services.AddSingleton<IEmailSender>(Emails);

            services.RemoveAll<INotificationRealtime>();
            services.AddSingleton<INotificationRealtime>(Realtime);

            services.AddHttpClient(MoMoGateway.HttpClientName)
                .ConfigurePrimaryHttpMessageHandler(() => MoMoApi);

            if (googleTokens is not null)
            {
                services.RemoveAll<IGoogleTokenVerifier>();
                services.AddSingleton(googleTokens);
            }

            if (imageStorage is not null)
            {
                services.RemoveAll<IImageStorage>();
                services.AddSingleton(imageStorage);
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
/// MoMo's create-payment endpoint. Answers with a payment URL the way MoMo does, and remembers the
/// request so a test can check the fields and the signature that went out.
/// </summary>
public sealed class RecordingMoMoApi : HttpMessageHandler
{
    private string? _lastRequest;

    /// <summary>Set to a non-zero code to make MoMo refuse to open the payment.</summary>
    public int ResultCode { get; set; }

    public string PayUrl { get; set; } = "https://test-payment.momo.vn/pay/hosted-payment-page";

    public JsonElement LastRequest => JsonDocument
        .Parse(_lastRequest ?? throw new InvalidOperationException("MoMo was never called."))
        .RootElement;

    public bool WasCalled => _lastRequest is not null;

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        _lastRequest = request.Content is null
            ? "{}"
            : await request.Content.ReadAsStringAsync(cancellationToken);

        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new
            {
                resultCode = ResultCode,
                message = ResultCode == 0 ? "Successful." : "Refused.",
                payUrl = ResultCode == 0 ? PayUrl : string.Empty
            })
        };
    }
}

/// <summary>Keeps every realtime push, so a test can assert who was told and what they were told.</summary>
public sealed class RecordingNotificationRealtime : INotificationRealtime
{
    private readonly ConcurrentQueue<(Guid UserId, NotificationResponse Notification)> _pushed = new();

    public Task PushAsync(
        Guid userId,
        NotificationResponse notification,
        CancellationToken cancellationToken = default)
    {
        _pushed.Enqueue((userId, notification));

        return Task.CompletedTask;
    }

    public IReadOnlyList<NotificationResponse> PushedTo(Guid userId) =>
        [.. _pushed.Where(p => p.UserId == userId).Select(p => p.Notification)];
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

/// <summary>
/// Stands in for Cloudinary. Records what was uploaded and what was deleted, so a test can assert
/// that removing a listing image also removes the remote file.
/// </summary>
public sealed class RecordingImageStorage : IImageStorage
{
    private readonly ConcurrentQueue<string> _deleted = new();

    public Task<StoredImage> UploadAsync(
        ImageUpload upload,
        string folder,
        CancellationToken cancellationToken = default)
    {
        var publicId = $"{folder}/{Guid.NewGuid():N}";

        return Task.FromResult(
            new StoredImage($"https://images.test/{publicId}.jpg", publicId, 800, 600));
    }

    public Task DeleteAsync(string publicId, CancellationToken cancellationToken = default)
    {
        _deleted.Enqueue(publicId);

        return Task.CompletedTask;
    }

    public bool WasDeleted(string publicId) => _deleted.Contains(publicId);
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
