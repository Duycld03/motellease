using MotelLease.Domain.Enums;

namespace MotelLease.Application.Accounts.Contracts;

public sealed record ProfileResponse(
    Guid Id,
    string Username,
    string Email,
    string FullName,
    string? PhoneNumber,
    Gender Gender,
    UserRole Role,
    string? AvatarUrl,
    string PreferredLanguage,
    bool EmailConfirmed,
    bool HasPassword,
    bool IsGoogleLinked,
    DateTimeOffset CreatedAt,
    OwnerProfileResponse? OwnerProfile,
    StaffProfileResponse? StaffProfile);

public sealed record OwnerProfileResponse(
    BusinessType BusinessType,
    string? BusinessName,
    string? BankName,
    string? BankAccountNumber,
    string? BankAccountHolder,
    decimal AvailableBalance);

public sealed record StaffProfileResponse(DateOnly HireDate, int ActiveAssignmentCount);

public sealed record UpdateProfileRequest(string FullName, string? PhoneNumber, Gender Gender);

public sealed record UpdateLanguageRequest(string Language);

public sealed record UpdateAvatarRequest(
    Stream Content,
    string FileName,
    string ContentType,
    long Length);

public sealed record AvatarResponse(string AvatarUrl);

public sealed record SendEmailChangeOtpRequest(string NewEmail);

public sealed record VerifyEmailChangeOtpRequest(string NewEmail, string Code);

/// <summary>
/// One row per refresh token still usable. <paramref name="IsCurrent"/> marks the token the
/// caller is holding, so the UI can label it and avoid revoking itself by accident.
/// </summary>
public sealed record SessionResponse(
    Guid Id,
    string? UserAgent,
    string? IpAddress,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt,
    bool IsCurrent);
