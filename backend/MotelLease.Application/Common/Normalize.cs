namespace MotelLease.Application.Common;

/// <summary>
/// Canonical forms for values that are compared or made unique. Applied in handlers rather
/// than in EF configurations so the same rule holds for cache keys and emails too.
/// </summary>
public static class Normalize
{
    /// <summary>
    /// Lower-cased and trimmed. The unique index on Users.Email is case-sensitive, so the
    /// canonical form has to be established before the row is written, not after.
    /// </summary>
    public static string Email(string email) => email.Trim().ToLowerInvariant();

    public static string Username(string username) => username.Trim().ToLowerInvariant();

    /// <summary>Collapses inner whitespace so "Nguyen  Van A" and "Nguyen Van A" match.</summary>
    public static string FullName(string fullName) =>
        string.Join(' ', fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries |
                                             StringSplitOptions.TrimEntries));

    public static string? PhoneNumber(string? phoneNumber) =>
        string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber.Trim();

    public static string Language(string language) => language.Trim().ToLowerInvariant();
}
