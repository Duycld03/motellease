using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using MotelLease.Application.Common.Abstractions;
using MotelLease.Domain.Entities;

namespace MotelLease.Infrastructure.Security;

public sealed class JwtTokenService : ITokenService
{
    private readonly JwtOptions _options;
    private readonly TimeProvider _clock;
    private readonly SigningCredentials _credentials;
    private readonly JsonWebTokenHandler _handler = new();

    public JwtTokenService(IOptions<JwtOptions> options, TimeProvider clock)
    {
        _options = options.Value;
        _clock = clock;
        _credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey)),
            SecurityAlgorithms.HmacSha256);
    }

    public TimeSpan RefreshTokenLifetime => TimeSpan.FromDays(_options.RefreshTokenDays);

    public AccessToken CreateAccessToken(User user, Guid sessionId)
    {
        var issuedAt = _clock.GetUtcNow();
        var expiresAt = issuedAt.AddMinutes(_options.AccessTokenMinutes);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            IssuedAt = issuedAt.UtcDateTime,
            NotBefore = issuedAt.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            SigningCredentials = _credentials,
            Claims = new Dictionary<string, object>
            {
                [JwtRegisteredClaimNames.Sub] = user.Id.ToString(),
                [JwtRegisteredClaimNames.Role] = user.Role.ToString(),
                // The session this token belongs to, so /me/sessions can mark the current
                // device and a password change can spare it.
                [JwtRegisteredClaimNames.Sid] = sessionId.ToString(),
                [JwtRegisteredClaimNames.Email] = user.Email,
                [JwtRegisteredClaimNames.Language] = user.PreferredLanguage
            }
        };

        return new AccessToken(
            _handler.CreateToken(descriptor),
            expiresAt,
            _options.AccessTokenMinutes * 60);
    }

    /// <summary>
    /// 256 bits of randomness, so the token needs no signature: it is unguessable and only
    /// meaningful against the stored hash.
    /// </summary>
    public RefreshTokenPair CreateRefreshToken()
    {
        var raw = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(32));

        return new RefreshTokenPair(raw, HashRefreshToken(raw));
    }

    /// <summary>
    /// Plain SHA-256, not BCrypt: the input is already high-entropy random, and lookup by
    /// hash has to be a single indexed equality match rather than a scan over every row.
    /// </summary>
    public string HashRefreshToken(string rawRefreshToken) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(rawRefreshToken)));
}

/// <summary>
/// Claim names as short strings rather than ClaimTypes URIs: the token stays small and
/// readable, and the Api layer reads the same names back with MapInboundClaims off.
/// </summary>
public static class JwtRegisteredClaimNames
{
    public const string Sub = "sub";
    public const string Role = "role";
    public const string Sid = "sid";
    public const string Email = "email";
    public const string Language = "lang";
}
