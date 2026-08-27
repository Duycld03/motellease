using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using MotelLease.Application.Auth.Contracts;
using MotelLease.Domain.Enums;

namespace MotelLease.Tests.Integration;

/// <summary>
/// The HTTP plumbing every flow test needs: a bearer request, the problem+json key behind a
/// failure, and an account to act as. Shared so a test reads as the flow it describes.
/// </summary>
internal static class ApiRequests
{
    /// <summary>Mirrors the API's own serialization: camelCase, enums as names.</summary>
    internal static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    internal const string Password = "Passw0rd123";

    internal static Task<HttpResponseMessage> SendAsync(
        this HttpClient client,
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

        return client.SendAsync(request);
    }

    internal static async Task<T> ReadAsync<T>(this HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<T>(Json)
        ?? throw new InvalidOperationException($"Empty {typeof(T).Name} body.");

    /// <summary>The message key behind a problem+json response.</summary>
    internal static async Task<string?> ReadCodeAsync(this HttpResponseMessage response)
    {
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();

        return problem.TryGetProperty("code", out var code) ? code.GetString() : null;
    }

    /// <summary>
    /// Registers through the real three-step flow, reading the OTP out of the recorded email.
    /// Only Tenant and Owner are self-assignable; staff accounts are seeded by the test that
    /// needs one.
    /// </summary>
    internal static async Task<string> RegisterAsync(
        this HttpClient client,
        RecordingEmailSender emails,
        UserRole role)
    {
        var email = $"user-{Guid.NewGuid():N}@example.com";

        await client.PostAsJsonAsync(
            "/api/v1/auth/register/send-otp", new SendRegistrationOtpRequest(email));

        await client.PostAsJsonAsync(
            "/api/v1/auth/register/verify-otp",
            new VerifyRegistrationOtpRequest(email, emails.LastCodeFor(email)));

        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterRequest(
                Username: $"u{Guid.NewGuid():N}"[..16],
                Email: email,
                Password: Password,
                FullName: "Nguyen Van A",
                PhoneNumber: "0912345678",
                Gender: Gender.Male,
                Role: role,
                PreferredLanguage: "vi"),
            Json);

        response.EnsureSuccessStatusCode();

        return (await response.ReadAsync<AuthTokensResponse>()).AccessToken;
    }

    internal static async Task<string> LoginAsync(this HttpClient client, string email)
    {
        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/login", new LoginRequest(email, Password));

        response.EnsureSuccessStatusCode();

        return (await response.ReadAsync<AuthTokensResponse>()).AccessToken;
    }
}
