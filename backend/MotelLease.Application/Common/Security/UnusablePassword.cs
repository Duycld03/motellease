using System.Security.Cryptography;

namespace MotelLease.Application.Common.Security;

/// <summary>
/// A Google-only account still needs a value in the required PasswordHash column. Storing a
/// marked random string keeps "signed up with Google" distinguishable from "has a password",
/// which a bare empty string could not do. The marker is a character BCrypt never emits, so
/// it can never collide with a real hash.
/// </summary>
public static class UnusablePassword
{
    private const char Marker = '!';

    public static string Create() =>
        Marker + Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    public static bool IsUnusable(string passwordHash) =>
        passwordHash.Length == 0 || passwordHash[0] == Marker;
}
