using MotelLease.Domain.Enums;

namespace MotelLease.Application.Auth.Contracts;

public sealed record SendRegistrationOtpRequest(string Email);

public sealed record VerifyRegistrationOtpRequest(string Email, string Code);

/// <summary>
/// Runs after the address was verified, so no code is passed here: ownership of
/// <paramref name="Email"/> is already on file (see IVerifiedEmailStore).
/// </summary>
public sealed record RegisterRequest(
    string Username,
    string Email,
    string Password,
    string FullName,
    string? PhoneNumber,
    Gender Gender,
    UserRole Role,
    string? PreferredLanguage);

/// <summary>Accepts either the username or the email in <paramref name="Login"/>.</summary>
public sealed record LoginRequest(string Login, string Password);

/// <summary>
/// <paramref name="Role"/> only applies the first time this Google account signs in; an
/// existing account keeps the role it already has.
/// </summary>
public sealed record GoogleLoginRequest(string IdToken, UserRole? Role);

public sealed record RefreshTokenRequest(string RefreshToken);

public sealed record LogoutRequest(string RefreshToken);

public sealed record ForgotPasswordRequest(string Email);

public sealed record ResetPasswordRequest(string Email, string Code, string NewPassword);

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public sealed record AuthTokensResponse(
    string AccessToken,
    string TokenType,
    int ExpiresIn,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt,
    AuthenticatedUser User);

public sealed record AuthenticatedUser(
    Guid Id,
    string Username,
    string Email,
    string FullName,
    UserRole Role,
    string? AvatarUrl,
    string PreferredLanguage,
    bool EmailConfirmed);

/// <summary>
/// Deliberately says nothing about whether the address exists — an attacker must not be
/// able to enumerate accounts through this endpoint.
/// </summary>
public sealed record OtpSentResponse(int ExpiresInSeconds);

public sealed record OtpVerifiedResponse(bool Verified, int ValidForSeconds);
