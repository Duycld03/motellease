using System.Globalization;
using System.Security.Claims;
using MotelLease.Application.Common.Abstractions;
using MotelLease.Application.Common.Security;
using MotelLease.Domain.Enums;
using MotelLease.Infrastructure.Security;

namespace MotelLease.Api.Authentication;

/// <summary>
/// Reads the caller from the validated access token. Claims only — anything that can change
/// after the token was issued (a lock, a revoked staff assignment) is checked against the
/// database by the handler that cares (docs/domain-rules.md §6).
/// </summary>
public sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;

    public Guid? UserId =>
        Guid.TryParse(Principal?.FindFirstValue(JwtRegisteredClaimNames.Sub), out var id)
            ? id
            : null;

    public UserRole? Role =>
        Enum.TryParse<UserRole>(
            Principal?.FindFirstValue(JwtRegisteredClaimNames.Role),
            out var role)
            ? role
            : null;

    public Guid? SessionId =>
        Guid.TryParse(Principal?.FindFirstValue(JwtRegisteredClaimNames.Sid), out var sessionId)
            ? sessionId
            : null;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    public Guid RequireUserId() =>
        UserId ?? throw new InvalidOperationException(
            "No authenticated user on this request. The endpoint is missing [Authorize].");
}

public sealed class RequestContext(IHttpContextAccessor accessor) : IRequestContext
{
    public string? UserAgent => accessor.HttpContext?.Request.Headers.UserAgent.ToString();

    /// <summary>
    /// Behind a reverse proxy this is only right when forwarded headers are configured;
    /// otherwise it is the proxy's address. Recorded for the session list, never for access
    /// decisions.
    /// </summary>
    public string? IpAddress => accessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    /// <summary>
    /// Resolved by the localization middleware from Accept-Language, already narrowed to a
    /// supported culture.
    /// </summary>
    public string Language
    {
        get
        {
            var culture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

            return SupportedLanguages.IsSupported(culture)
                ? culture
                : SupportedLanguages.Default;
        }
    }
}
