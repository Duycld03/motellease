using MotelLease.Domain.Common;
using MotelLease.Domain.Enums;

namespace MotelLease.Domain.Entities;

public class User : Entity, ISoftDeletable
{
    public string Username { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string? PhoneNumber { get; set; }
    public Gender Gender { get; set; } = Gender.Other;
    public UserRole Role { get; set; }

    /// <summary>Google subject id. Null for password-only accounts.</summary>
    public string? SocialId { get; set; }

    public string? AvatarUrl { get; set; }
    public string? AvatarPublicId { get; set; }

    /// <summary>ISO 639-1. Drives the language of emails and notifications.</summary>
    public string PreferredLanguage { get; set; } = "vi";

    public bool EmailConfirmed { get; set; }
    public bool IsLocked { get; set; }
    public string? LockedReason { get; set; }
    public bool IsDeleted { get; set; }

    public OwnerProfile? OwnerProfile { get; set; }
    public StaffProfile? StaffProfile { get; set; }
}

/// <summary>
/// Owner-specific fields. Replaces the Mongoose discriminator of the original project.
/// </summary>
public class OwnerProfile : Entity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public BusinessType BusinessType { get; set; } = BusinessType.Individual;
    public string? BusinessName { get; set; }

    public string? BankName { get; set; }
    public string? BankAccountNumber { get; set; }
    public string? BankAccountHolder { get; set; }

    /// <summary>
    /// Withdrawable balance. A withdraw request may never exceed this
    /// (docs/domain-rules.md §9.11).
    /// </summary>
    public decimal AvailableBalance { get; set; }
}

public class StaffProfile : Entity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public DateOnly HireDate { get; set; }

    /// <summary>The owner who created this staff account.</summary>
    public Guid CreatedByUserId { get; set; }
}
