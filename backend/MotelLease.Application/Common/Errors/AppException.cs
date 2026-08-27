namespace MotelLease.Application.Common.Errors;

/// <summary>
/// Carries a resource key, not a sentence: the Api layer renders it in the request
/// language and returns RFC 7807 problem+json (docs/api-design.md, Conventions).
/// </summary>
public abstract class AppException(string messageKey, params object[] arguments) : Exception(messageKey)
{
    public string MessageKey { get; } = messageKey;

    public object[] Arguments { get; } = arguments;

    public abstract int StatusCode { get; }
}

public sealed class NotFoundException(string messageKey, params object[] arguments)
    : AppException(messageKey, arguments)
{
    public override int StatusCode => StatusCodes.NotFound;
}

/// <summary>The request contradicts existing state — a taken email, a used token.</summary>
public sealed class ConflictException(string messageKey, params object[] arguments)
    : AppException(messageKey, arguments)
{
    public override int StatusCode => StatusCodes.Conflict;
}

/// <summary>Bad credentials or a token that cannot be trusted.</summary>
public sealed class AuthenticationException(string messageKey, params object[] arguments)
    : AppException(messageKey, arguments)
{
    public override int StatusCode => StatusCodes.Unauthorized;
}

/// <summary>Authenticated, but not allowed to touch this resource.</summary>
public sealed class ForbiddenException(string messageKey, params object[] arguments)
    : AppException(messageKey, arguments)
{
    public override int StatusCode => StatusCodes.Forbidden;
}

/// <summary>
/// Throttled. <see cref="RetryAfter"/> becomes the Retry-After header so a client can
/// back off instead of hammering the OTP endpoints.
/// </summary>
public sealed class TooManyRequestsException(
    TimeSpan retryAfter,
    string messageKey,
    params object[] arguments)
    : AppException(messageKey, arguments)
{
    public TimeSpan RetryAfter { get; } = retryAfter;

    public override int StatusCode => StatusCodes.TooManyRequests;
}

/// <summary>
/// A rule that input validation cannot express, checked inside the handler — for example
/// a registration attempt for an address that never received an OTP.
/// </summary>
public sealed class BusinessRuleException(string messageKey, params object[] arguments)
    : AppException(messageKey, arguments)
{
    public override int StatusCode => StatusCodes.UnprocessableEntity;
}

/// <summary>
/// Duplicated rather than referenced from ASP.NET: Application must not depend on the web
/// framework (CLAUDE.md, Layering).
/// </summary>
internal static class StatusCodes
{
    internal const int Unauthorized = 401;
    internal const int Forbidden = 403;
    internal const int NotFound = 404;
    internal const int Conflict = 409;
    internal const int UnprocessableEntity = 422;
    internal const int TooManyRequests = 429;
}
