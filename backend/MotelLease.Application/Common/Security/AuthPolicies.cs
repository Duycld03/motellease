using MotelLease.Domain.Enums;

namespace MotelLease.Application.Common.Security;

/// <summary>
/// Role policies, layer one of authorization (docs/domain-rules.md §6). Resource-level
/// checks are separate handlers — a role alone never proves access to a given boarding
/// house, which is exactly what the old middlewares got wrong.
/// </summary>
public static class AuthPolicies
{
    public const string RequireOwner = nameof(RequireOwner);
    public const string RequireStaffOrOwner = nameof(RequireStaffOrOwner);
    public const string RequireAdmin = nameof(RequireAdmin);
    public const string RequireTenant = nameof(RequireTenant);
}

public static class SupportedLanguages
{
    public const string Default = "vi";

    public static readonly string[] All = ["vi", "en"];

    public static bool IsSupported(string? language) =>
        language is not null && All.Contains(language, StringComparer.OrdinalIgnoreCase);
}

/// <summary>
/// Roles a visitor may pick when registering. Staff accounts are created by an owner and
/// admins are seeded or created by another admin (docs/features.md §1).
/// </summary>
public static class SelfAssignableRoles
{
    public static bool IsAllowed(UserRole role) => role is UserRole.Tenant or UserRole.Owner;
}
