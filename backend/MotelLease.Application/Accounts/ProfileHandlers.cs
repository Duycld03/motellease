using Microsoft.EntityFrameworkCore;
using MotelLease.Application.Accounts.Contracts;
using MotelLease.Application.Common;
using MotelLease.Application.Common.Abstractions;
using MotelLease.Application.Common.Errors;
using MotelLease.Application.Common.Security;

namespace MotelLease.Application.Accounts;

/// <summary>GET /me.</summary>
public sealed class GetProfileHandler(IAppDbContext database, ICurrentUser currentUser)
{
    public async Task<ProfileResponse> HandleAsync(CancellationToken cancellationToken = default)
    {
        var userId = currentUser.RequireUserId();

        var user = await database.Users
            .AsNoTracking()
            .Include(u => u.OwnerProfile)
            .Include(u => u.StaffProfile)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new NotFoundException(MessageKeys.Account.NotFound);

        // Staff see how many houses they currently cover; the count is the live assignments,
        // never the historical ones (docs/domain-rules.md §9.12).
        var activeAssignments = user.StaffProfile is null
            ? 0
            : await database.StaffAssignments
                .CountAsync(
                    a => a.StaffUserId == user.Id && a.UnassignedAt == null,
                    cancellationToken);

        return new ProfileResponse(
            user.Id,
            user.Username,
            user.Email,
            user.FullName,
            user.PhoneNumber,
            user.Gender,
            user.Role,
            user.AvatarUrl,
            user.PreferredLanguage,
            user.EmailConfirmed,
            !UnusablePassword.IsUnusable(user.PasswordHash),
            user.SocialId is not null,
            user.CreatedAt,
            user.OwnerProfile is null
                ? null
                : new OwnerProfileResponse(
                    user.OwnerProfile.BusinessType,
                    user.OwnerProfile.BusinessName,
                    user.OwnerProfile.BankName,
                    user.OwnerProfile.BankAccountNumber,
                    user.OwnerProfile.BankAccountHolder,
                    user.OwnerProfile.AvailableBalance),
            user.StaffProfile is null
                ? null
                : new StaffProfileResponse(user.StaffProfile.HireDate, activeAssignments));
    }
}

/// <summary>
/// PUT /me. Email, username and role are not editable here — each has its own flow or is
/// administrative.
/// </summary>
public sealed class UpdateProfileHandler(
    IAppDbContext database,
    ICurrentUser currentUser,
    GetProfileHandler getProfile)
{
    public async Task<ProfileResponse> HandleAsync(
        UpdateProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUser.RequireUserId();

        var user = await database.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new NotFoundException(MessageKeys.Account.NotFound);

        user.FullName = Normalize.FullName(request.FullName);
        user.PhoneNumber = Normalize.PhoneNumber(request.PhoneNumber);
        user.Gender = request.Gender;

        await database.SaveChangesAsync(cancellationToken);

        return await getProfile.HandleAsync(cancellationToken);
    }
}

/// <summary>
/// PUT /me/language. Drives the language of emails and notifications, not just the UI — a
/// stored preference is the only thing a background job can read.
/// </summary>
public sealed class UpdateLanguageHandler(IAppDbContext database, ICurrentUser currentUser)
{
    public async Task HandleAsync(
        UpdateLanguageRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!SupportedLanguages.IsSupported(request.Language))
        {
            throw new BusinessRuleException(
                MessageKeys.Account.LanguageNotSupported,
                string.Join(", ", SupportedLanguages.All));
        }

        var userId = currentUser.RequireUserId();

        var user = await database.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new NotFoundException(MessageKeys.Account.NotFound);

        user.PreferredLanguage = Normalize.Language(request.Language);

        await database.SaveChangesAsync(cancellationToken);
    }
}
