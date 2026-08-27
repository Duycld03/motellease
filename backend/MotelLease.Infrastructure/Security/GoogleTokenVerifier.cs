using Google.Apis.Auth;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MotelLease.Application.Common.Abstractions;

namespace MotelLease.Infrastructure.Security;

/// <summary>
/// Verifies a Google ID token against Google's published signing keys. The old project
/// trusted whatever the browser posted back; here the token is checked server-side and the
/// audience is pinned to our own client id.
/// </summary>
public sealed class GoogleTokenVerifier(
    IOptions<GoogleAuthOptions> options,
    ILogger<GoogleTokenVerifier> logger) : IGoogleTokenVerifier
{
    private readonly GoogleAuthOptions _options = options.Value;

    public async Task<GoogleIdentity?> VerifyAsync(
        string idToken,
        CancellationToken cancellationToken = default)
    {
        var settings = new GoogleJsonWebSignature.ValidationSettings();

        if (!string.IsNullOrWhiteSpace(_options.ClientId))
        {
            settings.Audience = [_options.ClientId];
        }
        else
        {
            // Without an audience any Google-issued token would pass, including one minted
            // for an unrelated application. Refuse rather than accept that.
            logger.LogError(
                "GoogleAuth:ClientId is not configured; Google sign-in is refused.");

            return null;
        }

        try
        {
            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);

            return new GoogleIdentity(
                payload.Subject,
                payload.Email,
                payload.EmailVerified,
                payload.Name,
                payload.Picture);
        }
        catch (InvalidJwtException exception)
        {
            // Expected for an expired, forged or misaddressed token — information for the
            // log, never for the caller.
            logger.LogInformation(exception, "Google ID token rejected.");

            return null;
        }
    }
}
