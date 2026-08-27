using System.ComponentModel.DataAnnotations;

namespace MotelLease.Infrastructure.Security;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required]
    public string Issuer { get; set; } = null!;

    [Required]
    public string Audience { get; set; } = null!;

    /// <summary>
    /// Never committed: set through user-secrets locally and GitHub Actions secrets in CI
    /// (CLAUDE.md). At least 32 bytes, because HS256 keys shorter than the digest weaken it.
    /// </summary>
    [Required, MinLength(32)]
    public string SigningKey { get; set; } = null!;

    /// <summary>
    /// Short on purpose. Locking an account or revoking a session cannot reach a token
    /// already issued, so the window in which a revoked user still gets through is this long.
    /// </summary>
    [Range(1, 120)]
    public int AccessTokenMinutes { get; set; } = 15;

    [Range(1, 365)]
    public int RefreshTokenDays { get; set; } = 30;
}

public sealed class GoogleAuthOptions
{
    public const string SectionName = "GoogleAuth";

    /// <summary>
    /// The OAuth client id an ID token must be addressed to. Without this check a token
    /// minted for any other Google app would be accepted.
    /// </summary>
    public string? ClientId { get; set; }
}
