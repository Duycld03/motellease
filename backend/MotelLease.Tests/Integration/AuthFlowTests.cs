using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using MotelLease.Application.Accounts.Contracts;
using MotelLease.Application.Auth.Contracts;

namespace MotelLease.Tests.Integration;

/// <summary>
/// Drives the Auth group through HTTP against a real PostGIS database. These are the flows
/// the "Auth and accounts" section of docs/api-design.md describes; the security-relevant ones
/// (rotation, replay, revocation) are asserted rather than assumed.
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class AuthFlowTests : IAsyncLifetime
{
    // Mirrors the API's own serialization: camelCase, and enums as names rather than numbers.
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly PostgresFixture _postgres;
    private MotelLeaseAppFactory _app = null!;
    private HttpClient _client = null!;

    public AuthFlowTests(PostgresFixture postgres) => _postgres = postgres;

    public async Task InitializeAsync()
    {
        _app = new MotelLeaseAppFactory(_postgres.ConnectionString);
        await _app.MigrateAsync();
        _client = _app.CreateClient();
    }

    public Task DisposeAsync()
    {
        _client.Dispose();

        return _app.DisposeAsync().AsTask();
    }

    [Fact]
    public async Task Register_requires_a_verified_email()
    {
        var email = NewEmail();

        // No OTP was ever issued for this address, so step 3 must refuse.
        var response = await _client.PostAsJsonAsync(
            "/api/v1/auth/register",
            NewRegistration(email),
            Json);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal(
            "error.auth.email_not_verified",
            await ReadCodeAsync(response));
    }

    [Fact]
    public async Task Registration_flow_issues_a_usable_session()
    {
        var email = NewEmail();
        var tokens = await RegisterAsync(email);

        Assert.Equal("Bearer", tokens.TokenType);
        Assert.Equal(email, tokens.User.Email);
        Assert.True(tokens.User.EmailConfirmed);

        // The access token has to actually open an authenticated endpoint.
        var profile = await GetProfileAsync(tokens.AccessToken);

        Assert.Equal(email, profile.Email);
        Assert.True(profile.HasPassword);
        Assert.False(profile.IsGoogleLinked);
    }

    [Fact]
    public async Task Registering_a_taken_email_is_refused_before_any_otp_is_sent()
    {
        var email = NewEmail();
        await RegisterAsync(email);

        var response = await _client.PostAsJsonAsync(
            "/api/v1/auth/register/send-otp",
            new SendRegistrationOtpRequest(email));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Login_accepts_the_username_and_the_email()
    {
        var email = NewEmail();
        var registration = await RegisterAsync(email);

        foreach (var login in new[] { email, registration.User.Username })
        {
            var response = await _client.PostAsJsonAsync(
                "/api/v1/auth/login",
                new LoginRequest(login, Password));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    [Fact]
    public async Task Login_reports_the_same_failure_for_a_wrong_password_and_an_unknown_account()
    {
        var email = NewEmail();
        await RegisterAsync(email);

        var wrongPassword = await _client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest(email, "Wrong0rd123"));

        var unknownAccount = await _client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest(NewEmail(), Password));

        Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, unknownAccount.StatusCode);

        // Same code as well as same status: the response must not distinguish the two.
        Assert.Equal(await ReadCodeAsync(unknownAccount), await ReadCodeAsync(wrongPassword));
    }

    [Fact]
    public async Task Refresh_rotates_the_token_and_retires_the_old_one()
    {
        var registration = await RegisterAsync(NewEmail());

        var rotated = await RefreshAsync(registration.RefreshToken);

        Assert.NotEqual(registration.RefreshToken, rotated.RefreshToken);

        var replay = await _client.PostAsJsonAsync(
            "/api/v1/auth/refresh",
            new RefreshTokenRequest(registration.RefreshToken));

        Assert.Equal(HttpStatusCode.Unauthorized, replay.StatusCode);
        Assert.Equal("error.auth.refresh_token_reused", await ReadCodeAsync(replay));
    }

    [Fact]
    public async Task Replaying_a_refresh_token_revokes_the_whole_chain()
    {
        var registration = await RegisterAsync(NewEmail());
        var rotated = await RefreshAsync(registration.RefreshToken);

        // The attacker presents the stolen, already-rotated token...
        await _client.PostAsJsonAsync(
            "/api/v1/auth/refresh",
            new RefreshTokenRequest(registration.RefreshToken));

        // ...which must also cost the legitimate holder their live token.
        var afterReuse = await _client.PostAsJsonAsync(
            "/api/v1/auth/refresh",
            new RefreshTokenRequest(rotated.RefreshToken));

        Assert.Equal(HttpStatusCode.Unauthorized, afterReuse.StatusCode);
    }

    [Fact]
    public async Task Logout_revokes_only_the_device_that_asked()
    {
        var email = NewEmail();
        var first = await RegisterAsync(email);
        var second = await LoginAsync(email);

        var logout = await SendAsync(
            HttpMethod.Post,
            "/api/v1/auth/logout",
            first.AccessToken,
            new LogoutRequest(first.RefreshToken));

        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);

        var revoked = await _client.PostAsJsonAsync(
            "/api/v1/auth/refresh",
            new RefreshTokenRequest(first.RefreshToken));

        var untouched = await _client.PostAsJsonAsync(
            "/api/v1/auth/refresh",
            new RefreshTokenRequest(second.RefreshToken));

        Assert.Equal(HttpStatusCode.Unauthorized, revoked.StatusCode);
        Assert.Equal(HttpStatusCode.OK, untouched.StatusCode);

        // A token dropped by logout is merely invalid. Reporting it as a leak would let one
        // stale client sign every other device out.
        Assert.Equal("error.auth.refresh_token_invalid", await ReadCodeAsync(revoked));
    }

    [Fact]
    public async Task Changing_the_password_keeps_the_calling_session_and_drops_the_others()
    {
        var email = NewEmail();
        var caller = await RegisterAsync(email);
        var otherDevice = await LoginAsync(email);

        var change = await SendAsync(
            HttpMethod.Put,
            "/api/v1/auth/password",
            caller.AccessToken,
            new ChangePasswordRequest(Password, NewPassword));

        Assert.Equal(HttpStatusCode.NoContent, change.StatusCode);

        var callerStillValid = await _client.PostAsJsonAsync(
            "/api/v1/auth/refresh",
            new RefreshTokenRequest(caller.RefreshToken));

        var otherSignedOut = await _client.PostAsJsonAsync(
            "/api/v1/auth/refresh",
            new RefreshTokenRequest(otherDevice.RefreshToken));

        Assert.Equal(HttpStatusCode.OK, callerStillValid.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, otherSignedOut.StatusCode);

        // And the new password is the only one that works.
        var oldPassword = await _client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest(email, Password));

        var newPassword = await _client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest(email, NewPassword));

        Assert.Equal(HttpStatusCode.Unauthorized, oldPassword.StatusCode);
        Assert.Equal(HttpStatusCode.OK, newPassword.StatusCode);
    }

    [Fact]
    public async Task Forgot_password_answers_identically_for_an_unknown_address()
    {
        var known = NewEmail();
        await RegisterAsync(known);

        var unknown = NewEmail();

        var knownResponse = await _client.PostAsJsonAsync(
            "/api/v1/auth/password/forgot",
            new ForgotPasswordRequest(known));

        var unknownResponse = await _client.PostAsJsonAsync(
            "/api/v1/auth/password/forgot",
            new ForgotPasswordRequest(unknown));

        Assert.Equal(HttpStatusCode.OK, knownResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, unknownResponse.StatusCode);
        Assert.Equal(
            await knownResponse.Content.ReadAsStringAsync(),
            await unknownResponse.Content.ReadAsStringAsync());

        // The body is identical, and nothing was sent to the address that does not exist.
        Assert.True(_app.Emails.AnySentTo(known));
        Assert.False(_app.Emails.AnySentTo(unknown));
    }

    [Fact]
    public async Task Reset_password_signs_every_device_out()
    {
        var email = NewEmail();
        var session = await RegisterAsync(email);

        await _client.PostAsJsonAsync(
            "/api/v1/auth/password/forgot",
            new ForgotPasswordRequest(email));

        var reset = await _client.PostAsJsonAsync(
            "/api/v1/auth/password/reset",
            new ResetPasswordRequest(email, _app.Emails.LastCodeFor(email), NewPassword));

        Assert.Equal(HttpStatusCode.NoContent, reset.StatusCode);

        var oldSession = await _client.PostAsJsonAsync(
            "/api/v1/auth/refresh",
            new RefreshTokenRequest(session.RefreshToken));

        Assert.Equal(HttpStatusCode.Unauthorized, oldSession.StatusCode);
    }

    [Fact]
    public async Task A_wrong_otp_does_not_verify_the_address()
    {
        var email = NewEmail();

        await _client.PostAsJsonAsync(
            "/api/v1/auth/register/send-otp",
            new SendRegistrationOtpRequest(email));

        var wrong = await _client.PostAsJsonAsync(
            "/api/v1/auth/register/verify-otp",
            new VerifyRegistrationOtpRequest(email, NextWrongCode(_app.Emails.LastCodeFor(email))));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, wrong.StatusCode);
        Assert.Equal("error.otp.mismatch", await ReadCodeAsync(wrong));

        var register = await _client.PostAsJsonAsync(
            "/api/v1/auth/register",
            NewRegistration(email),
            Json);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, register.StatusCode);
    }

    [Fact]
    public async Task Sessions_list_marks_the_calling_device_and_can_revoke_another()
    {
        var email = NewEmail();
        var first = await RegisterAsync(email);
        var second = await LoginAsync(email);

        var sessions = await ReadSessionsAsync(second.AccessToken);

        Assert.Equal(2, sessions.Count);
        Assert.Single(sessions, s => s.IsCurrent);

        var other = sessions.Single(s => !s.IsCurrent);

        var revoke = await SendAsync(
            HttpMethod.Delete,
            $"/api/v1/me/sessions/{other.Id}",
            second.AccessToken);

        Assert.Equal(HttpStatusCode.NoContent, revoke.StatusCode);

        var revokedDevice = await _client.PostAsJsonAsync(
            "/api/v1/auth/refresh",
            new RefreshTokenRequest(first.RefreshToken));

        Assert.Equal(HttpStatusCode.Unauthorized, revokedDevice.StatusCode);
        Assert.Single(await ReadSessionsAsync(second.AccessToken));
    }

    [Fact]
    public async Task Email_change_needs_a_code_sent_to_the_new_address()
    {
        var email = NewEmail();
        var session = await RegisterAsync(email);
        var newEmail = NewEmail();

        var send = await SendAsync(
            HttpMethod.Post,
            "/api/v1/me/email/send-otp",
            session.AccessToken,
            new SendEmailChangeOtpRequest(newEmail));

        Assert.Equal(HttpStatusCode.OK, send.StatusCode);
        Assert.True(_app.Emails.AnySentTo(newEmail));

        var verify = await SendAsync(
            HttpMethod.Post,
            "/api/v1/me/email/verify-otp",
            session.AccessToken,
            new VerifyEmailChangeOtpRequest(newEmail, _app.Emails.LastCodeFor(newEmail)));

        Assert.Equal(HttpStatusCode.NoContent, verify.StatusCode);
        Assert.Equal(newEmail, (await GetProfileAsync(session.AccessToken)).Email);
    }

    [Fact]
    public async Task Authenticated_endpoints_refuse_an_absent_or_forged_token()
    {
        var anonymous = await _client.GetAsync("/api/v1/me");

        var forged = await SendAsync(
            HttpMethod.Get,
            "/api/v1/me",
            "eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiJub3BlIn0.not-a-real-signature");

        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, forged.StatusCode);
        Assert.Equal("error.unauthorized", await ReadCodeAsync(anonymous));
    }

    [Fact]
    public async Task Validation_failures_are_reported_per_field()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest(string.Empty, string.Empty));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(
            "error.validation.title",
            problem.GetProperty("code").GetString());
        Assert.True(problem.GetProperty("errors").TryGetProperty("Login", out _));
        Assert.True(problem.GetProperty("errors").TryGetProperty("Password", out _));
    }

    private const string Password = "Passw0rd123";
    private const string NewPassword = "Passw0rd456";

    private static string NewEmail() => $"user-{Guid.NewGuid():N}@example.com";

    private static RegisterRequest NewRegistration(string email) =>
        new(
            Username: $"u{Guid.NewGuid():N}"[..16],
            Email: email,
            Password: Password,
            FullName: "Nguyen Van A",
            PhoneNumber: "0912345678",
            Gender: Domain.Enums.Gender.Male,
            Role: Domain.Enums.UserRole.Tenant,
            PreferredLanguage: "vi");

    /// <summary>Any six digits other than the real code.</summary>
    private static string NextWrongCode(string code) =>
        code == "000000" ? "111111" : "000000";

    private async Task<AuthTokensResponse> RegisterAsync(string email)
    {
        await _client.PostAsJsonAsync(
            "/api/v1/auth/register/send-otp",
            new SendRegistrationOtpRequest(email));

        await _client.PostAsJsonAsync(
            "/api/v1/auth/register/verify-otp",
            new VerifyRegistrationOtpRequest(email, _app.Emails.LastCodeFor(email)));

        var response = await _client.PostAsJsonAsync(
            "/api/v1/auth/register",
            NewRegistration(email),
            Json);

        response.EnsureSuccessStatusCode();

        return await ReadAsync<AuthTokensResponse>(response);
    }

    private async Task<AuthTokensResponse> LoginAsync(string email)
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest(email, Password));

        response.EnsureSuccessStatusCode();

        return await ReadAsync<AuthTokensResponse>(response);
    }

    private async Task<AuthTokensResponse> RefreshAsync(string refreshToken)
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/auth/refresh",
            new RefreshTokenRequest(refreshToken));

        response.EnsureSuccessStatusCode();

        return await ReadAsync<AuthTokensResponse>(response);
    }

    private async Task<ProfileResponse> GetProfileAsync(string accessToken)
    {
        var response = await SendAsync(HttpMethod.Get, "/api/v1/me", accessToken);

        response.EnsureSuccessStatusCode();

        return await ReadAsync<ProfileResponse>(response);
    }

    private async Task<IReadOnlyList<SessionResponse>> ReadSessionsAsync(string accessToken)
    {
        var response = await SendAsync(HttpMethod.Get, "/api/v1/me/sessions", accessToken);

        response.EnsureSuccessStatusCode();

        return await ReadAsync<List<SessionResponse>>(response);
    }

    private Task<HttpResponseMessage> SendAsync(
        HttpMethod method,
        string path,
        string accessToken,
        object? body = null)
    {
        var request = new HttpRequestMessage(method, path)
        {
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", accessToken) }
        };

        if (body is not null)
        {
            request.Content = JsonContent.Create(body, options: Json);
        }

        return _client.SendAsync(request);
    }

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<T>(Json)
        ?? throw new InvalidOperationException($"Empty {typeof(T).Name} body.");

    /// <summary>The message key behind a problem+json response.</summary>
    private static async Task<string?> ReadCodeAsync(HttpResponseMessage response)
    {
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();

        return problem.TryGetProperty("code", out var code) ? code.GetString() : null;
    }
}
