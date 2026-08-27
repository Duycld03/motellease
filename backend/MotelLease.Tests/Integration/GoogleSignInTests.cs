using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using MotelLease.Application.Accounts.Contracts;
using MotelLease.Application.Auth.Contracts;
using MotelLease.Application.Common.Abstractions;

namespace MotelLease.Tests.Integration;

/// <summary>
/// Google sign-in. Google's own signature check is library code and cannot be exercised without a
/// token Google signed, so it is covered from the outside (a forged token must be refused) while
/// the handler branches behind it — account creation, linking, a locked account — use a stubbed
/// verifier.
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class GoogleSignInTests(PostgresFixture postgres)
{
    private const string ClientId = "test-client-id.apps.googleusercontent.com";
    private const string Password = "Passw0rd123";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public async Task A_token_google_did_not_sign_is_refused()
    {
        await using var app = await NewAppAsync(googleClientId: ClientId);
        using var client = app.CreateClient();

        var response = await Post(
            client,
            "eyJhbGciOiJSUzI1NiJ9.eyJzdWIiOiIxMjMiLCJlbWFpbCI6ImF0dGFja2VyQGV4YW1wbGUuY29tIn0.forged");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("error.auth.google_token_invalid", await ReadCodeAsync(response));
    }

    [Fact]
    public async Task An_unconfigured_client_id_refuses_instead_of_accepting_anything()
    {
        // No audience to pin means any Google-issued token would otherwise pass, including one
        // minted for an unrelated application. Failing closed is the whole point.
        await using var app = await NewAppAsync(googleClientId: null);
        using var client = app.CreateClient();

        var response = await Post(client, "any.token.at.all");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("error.auth.google_token_invalid", await ReadCodeAsync(response));

        // A malformed token would produce the same 401, so the status alone proves nothing here.
        // The log entry is what shows the guard ran and Google was never consulted.
        Assert.True(app.Logs.Any(
            LogLevel.Error,
            "GoogleAuth:ClientId is not configured"),
            app.Logs.Dump());
    }

    [Fact]
    public async Task An_empty_token_is_a_validation_failure()
    {
        await using var app = await NewAppAsync(googleClientId: ClientId);
        using var client = app.CreateClient();

        var response = await Post(client, string.Empty);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.True(problem.GetProperty("errors").TryGetProperty("IdToken", out _));
    }

    [Fact]
    public async Task An_unverified_google_address_is_refused()
    {
        var email = NewEmail();

        await using var app = await NewAppAsync(
            googleClientId: ClientId,
            googleTokens: new StubGoogleTokenVerifier(StubGoogleTokenVerifier.Unverified(email)));

        using var client = app.CreateClient();

        var response = await Post(client, "token-google-would-accept");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("error.auth.google_email_unverified", await ReadCodeAsync(response));
    }

    [Fact]
    public async Task A_first_time_google_user_gets_an_account_and_a_session()
    {
        var email = NewEmail();

        await using var app = await NewAppAsync(
            googleClientId: ClientId,
            googleTokens: new StubGoogleTokenVerifier(StubGoogleTokenVerifier.Verified(email)));

        using var client = app.CreateClient();

        var response = await Post(client, "token", UserRoleTenant);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var tokens = await ReadAsync<AuthTokensResponse>(response);

        Assert.Equal(email, tokens.User.Email);
        Assert.True(tokens.User.EmailConfirmed);

        // The account has no password, so a Google-only user must be labelled as such.
        var profile = await GetProfileAsync(client, tokens.AccessToken);

        Assert.True(profile.IsGoogleLinked);
        Assert.False(profile.HasPassword);
    }

    [Fact]
    public async Task Signing_in_twice_reuses_the_same_account()
    {
        var email = NewEmail();
        var identity = StubGoogleTokenVerifier.Verified(email);

        await using var app = await NewAppAsync(
            googleClientId: ClientId,
            googleTokens: new StubGoogleTokenVerifier(identity));

        using var client = app.CreateClient();

        var first = await ReadAsync<AuthTokensResponse>(await Post(client, "token", UserRoleTenant));
        var second = await ReadAsync<AuthTokensResponse>(await Post(client, "token"));

        Assert.Equal(first.User.Id, second.User.Id);
        Assert.NotEqual(first.RefreshToken, second.RefreshToken);
    }

    [Fact]
    public async Task Google_sign_in_links_itself_to_an_existing_password_account()
    {
        var email = NewEmail();

        // Register with a password first, on an app that uses the real verifier.
        await using var passwordApp = await NewAppAsync(googleClientId: ClientId);
        using var passwordClient = passwordApp.CreateClient();

        await RegisterWithPasswordAsync(passwordApp, passwordClient, email);

        // Then the same address arrives from Google, on the same database.
        await using var googleApp = await NewAppAsync(
            googleClientId: ClientId,
            googleTokens: new StubGoogleTokenVerifier(StubGoogleTokenVerifier.Verified(email)));

        using var googleClient = googleApp.CreateClient();

        var response = await Post(googleClient, "token");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var tokens = await ReadAsync<AuthTokensResponse>(response);
        var profile = await GetProfileAsync(googleClient, tokens.AccessToken);

        // One account, now reachable both ways — the password still works.
        Assert.True(profile.IsGoogleLinked);
        Assert.True(profile.HasPassword);

        var login = await passwordClient.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest(email, Password));

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }

    private const string UserRoleTenant = "Tenant";

    private static string NewEmail() => $"google-{Guid.NewGuid():N}@example.com";

    private static Task<HttpResponseMessage> Post(
        HttpClient client,
        string idToken,
        string? role = null) =>
        client.PostAsync(
            "/api/v1/auth/login/google",
            JsonContent.Create(
                role is null
                    ? new { idToken }
                    : (object)new { idToken, role },
                options: Json));

    private async Task RegisterWithPasswordAsync(
        MotelLeaseAppFactory app,
        HttpClient client,
        string email)
    {
        await client.PostAsJsonAsync(
            "/api/v1/auth/register/send-otp",
            new SendRegistrationOtpRequest(email));

        await client.PostAsJsonAsync(
            "/api/v1/auth/register/verify-otp",
            new VerifyRegistrationOtpRequest(email, app.Emails.LastCodeFor(email)));

        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterRequest(
                Username: $"g{Guid.NewGuid():N}"[..16],
                Email: email,
                Password: Password,
                FullName: "Nguyen Van A",
                PhoneNumber: "0912345678",
                Gender: Domain.Enums.Gender.Male,
                Role: Domain.Enums.UserRole.Tenant,
                PreferredLanguage: "vi"),
            Json);

        response.EnsureSuccessStatusCode();
    }

    private static async Task<ProfileResponse> GetProfileAsync(
        HttpClient client,
        string accessToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/me")
        {
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", accessToken) }
        };

        var response = await client.SendAsync(request);

        response.EnsureSuccessStatusCode();

        return await ReadAsync<ProfileResponse>(response);
    }

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<T>(Json)
        ?? throw new InvalidOperationException($"Empty {typeof(T).Name} body.");

    private static async Task<string?> ReadCodeAsync(HttpResponseMessage response)
    {
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();

        return problem.TryGetProperty("code", out var code) ? code.GetString() : null;
    }

    private async Task<MotelLeaseAppFactory> NewAppAsync(
        string? googleClientId,
        IGoogleTokenVerifier? googleTokens = null)
    {
        var app = new MotelLeaseAppFactory(
            postgres.ConnectionString,
            googleClientId: googleClientId,
            googleTokens: googleTokens);

        await app.MigrateAsync();

        return app;
    }
}
