using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MotelLease.Application.Auth.Contracts;

namespace MotelLease.Tests.Integration;

/// <summary>
/// The rate limiter is what stands between the credential endpoints and an offline password
/// guess, so its behaviour is asserted rather than assumed. Each fact gets its own app instance:
/// limiter state lives in the DI container, and every request in a run shares one partition
/// because TestServer supplies no remote address.
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class RateLimitTests(PostgresFixture postgres)
{
    private const int AuthLimit = 4;
    private const int OtpLimit = 3;

    [Fact]
    public async Task Login_is_throttled_once_the_window_is_spent()
    {
        await using var app = await NewAppAsync();
        using var client = app.CreateClient();

        var statuses = new List<HttpStatusCode>();

        for (var attempt = 0; attempt < AuthLimit + 2; attempt++)
        {
            var response = await client.PostAsJsonAsync(
                "/api/v1/auth/login",
                new LoginRequest("nobody@example.com", "Passw0rd123"));

            statuses.Add(response.StatusCode);
        }

        // The permitted attempts fail on credentials, not on the limiter.
        Assert.All(
            statuses.Take(AuthLimit),
            status => Assert.Equal(HttpStatusCode.Unauthorized, status));

        // Everything past the window is refused without reaching the handler.
        Assert.All(
            statuses.Skip(AuthLimit),
            status => Assert.Equal(HttpStatusCode.TooManyRequests, status));
    }

    [Fact]
    public async Task A_throttled_response_is_problem_json_and_says_when_to_retry()
    {
        await using var app = await NewAppAsync();
        using var client = app.CreateClient();

        HttpResponseMessage? throttled = null;

        for (var attempt = 0; attempt < AuthLimit + 1; attempt++)
        {
            throttled = await client.PostAsJsonAsync(
                "/api/v1/auth/login",
                new LoginRequest("nobody@example.com", "Passw0rd123"));
        }

        Assert.NotNull(throttled);
        Assert.Equal(HttpStatusCode.TooManyRequests, throttled.StatusCode);

        // A client that cannot tell how long to wait will simply retry immediately.
        Assert.NotNull(throttled.Content.Headers.ContentType);
        Assert.Equal("application/problem+json", throttled.Content.Headers.ContentType.MediaType);
        Assert.True(throttled.Headers.TryGetValues("Retry-After", out var retryAfter));
        Assert.True(int.Parse(retryAfter.Single()) > 0);

        var problem = await throttled.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal("error.rate_limited", problem.GetProperty("code").GetString());
        Assert.Equal(429, problem.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task The_otp_endpoints_have_a_budget_of_their_own()
    {
        await using var app = await NewAppAsync();
        using var client = app.CreateClient();

        // Spend the whole authentication window.
        for (var attempt = 0; attempt <= AuthLimit; attempt++)
        {
            await client.PostAsJsonAsync(
                "/api/v1/auth/login",
                new LoginRequest("nobody@example.com", "Passw0rd123"));
        }

        // A separate policy, so sending an OTP must still be allowed. Sharing one bucket would
        // let a login flood lock legitimate users out of registering.
        var otp = await client.PostAsJsonAsync(
            "/api/v1/auth/register/send-otp",
            new SendRegistrationOtpRequest(NewEmail()));

        Assert.Equal(HttpStatusCode.OK, otp.StatusCode);
    }

    [Fact]
    public async Task Sending_otps_is_throttled_separately_from_login()
    {
        await using var app = await NewAppAsync();
        using var client = app.CreateClient();

        for (var attempt = 0; attempt < OtpLimit; attempt++)
        {
            var allowed = await client.PostAsJsonAsync(
                "/api/v1/auth/register/send-otp",
                new SendRegistrationOtpRequest(NewEmail()));

            Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
        }

        // A fresh address each time, so the per-email cooldown is not what refuses this one.
        var throttled = await client.PostAsJsonAsync(
            "/api/v1/auth/register/send-otp",
            new SendRegistrationOtpRequest(NewEmail()));

        Assert.Equal(HttpStatusCode.TooManyRequests, throttled.StatusCode);

        // The per-email resend cooldown also answers 429, so pin down which one refused this:
        // it must be the per-IP limiter, otherwise this test would pass without a limiter at all.
        var problem = await throttled.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal("error.rate_limited", problem.GetProperty("code").GetString());

        // Login still works: exhausting the OTP window must not close the door on signing in.
        var login = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest("nobody@example.com", "Passw0rd123"));

        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
    }

    [Fact]
    public async Task Endpoints_outside_the_policies_are_not_throttled()
    {
        await using var app = await NewAppAsync();
        using var client = app.CreateClient();

        for (var attempt = 0; attempt <= AuthLimit; attempt++)
        {
            await client.PostAsJsonAsync(
                "/api/v1/auth/login",
                new LoginRequest("nobody@example.com", "Passw0rd123"));
        }

        // The limiter is opt-in per endpoint. A spent credential window must not take the
        // liveness probe or the authenticated surface down with it.
        var health = await client.GetAsync("/health");
        var me = await client.GetAsync("/api/v1/me");

        Assert.Equal(HttpStatusCode.OK, health.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);
    }

    private static string NewEmail() => $"user-{Guid.NewGuid():N}@example.com";

    /// <summary>
    /// A fresh app per fact, so each one starts with an empty window. Migrations are applied
    /// because the login attempts reach the database before failing on credentials.
    /// </summary>
    private async Task<MotelLeaseAppFactory> NewAppAsync()
    {
        var app = new MotelLeaseAppFactory(
            postgres.ConnectionString,
            authPermitLimit: AuthLimit,
            otpPermitLimit: OtpLimit);

        await app.MigrateAsync();

        return app;
    }
}
