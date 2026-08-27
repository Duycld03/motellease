namespace MotelLease.Application.Common.Abstractions;

public interface IPasswordHasher
{
    string Hash(string password);

    /// <summary>
    /// False for a wrong password and for a malformed stored hash alike — a corrupted hash
    /// must not be treated as "no password required".
    /// </summary>
    bool Verify(string password, string passwordHash);
}
