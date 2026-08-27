using MotelLease.Domain.Enums;

namespace MotelLease.Application.Common.Abstractions;

/// <summary>The authenticated caller, read from the access token claims.</summary>
public interface ICurrentUser
{
    Guid? UserId { get; }

    UserRole? Role { get; }

    /// <summary>The RefreshTokens row this access token was issued from (<c>sid</c> claim).</summary>
    Guid? SessionId { get; }

    bool IsAuthenticated { get; }

    /// <summary>
    /// The caller's id, or a failure if the endpoint was reached unauthenticated. Handlers
    /// behind an [Authorize] attribute use this instead of null-checking every time.
    /// </summary>
    Guid RequireUserId();
}

/// <summary>
/// Per-request metadata that session management needs. Kept apart from ICurrentUser
/// because it is also available on anonymous requests such as login.
/// </summary>
public interface IRequestContext
{
    string? UserAgent { get; }

    string? IpAddress { get; }

    /// <summary>ISO 639-1 language resolved from Accept-Language. Never null.</summary>
    string Language { get; }
}
