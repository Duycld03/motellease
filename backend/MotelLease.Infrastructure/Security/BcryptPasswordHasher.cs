using MotelLease.Application.Common.Abstractions;

namespace MotelLease.Infrastructure.Security;

/// <summary>
/// BCrypt with a work factor high enough to make offline guessing expensive. The factor is
/// stored inside the hash, so raising it later leaves existing hashes verifiable.
/// </summary>
public sealed class BcryptPasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12;

    public string Hash(string password) =>
        BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);

    public bool Verify(string password, string passwordHash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, passwordHash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            // A stored value that is not a BCrypt hash — the unusable-password marker of a
            // Google-only account, or corruption. Neither may pass as a match.
            return false;
        }
    }
}
