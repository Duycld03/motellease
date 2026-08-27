using Microsoft.EntityFrameworkCore;
using MotelLease.Application.Auth.Contracts;
using MotelLease.Application.Common;
using MotelLease.Application.Common.Abstractions;
using MotelLease.Application.Common.Errors;
using MotelLease.Application.Common.Security;
using MotelLease.Domain.Entities;
using MotelLease.Domain.Enums;

namespace MotelLease.Application.Auth;

/// <summary>
/// POST /auth/register. Requires the address to have passed OTP verification already, so
/// the account starts with EmailConfirmed = true and no "unverified account" state exists.
/// </summary>
public sealed class RegisterHandler(
    IAppDbContext database,
    IPasswordHasher passwordHasher,
    IVerifiedEmailStore verifiedEmails,
    SessionIssuer sessionIssuer)
{
    public async Task<AuthTokensResponse> HandleAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        // Staff accounts are created by an owner, admins are seeded — neither is reachable
        // from a public form (docs/features.md §1).
        if (!SelfAssignableRoles.IsAllowed(request.Role))
        {
            throw new ForbiddenException(MessageKeys.Auth.RoleNotSelfAssignable);
        }

        var email = Normalize.Email(request.Email);
        var username = Normalize.Username(request.Username);

        if (!await verifiedEmails.IsVerifiedAsync(email, cancellationToken))
        {
            throw new BusinessRuleException(MessageKeys.Auth.EmailNotVerified);
        }

        // Both checks up front so the caller learns which field is the problem; the partial
        // unique indexes are still the authority if two requests race.
        if (await database.Users.AnyAsync(u => u.Email == email, cancellationToken))
        {
            throw new ConflictException(MessageKeys.Auth.EmailTaken);
        }

        if (await database.Users.AnyAsync(u => u.Username == username, cancellationToken))
        {
            throw new ConflictException(MessageKeys.Auth.UsernameTaken);
        }

        var language = request.PreferredLanguage is { } requested
                       && SupportedLanguages.IsSupported(requested)
            ? Normalize.Language(requested)
            : SupportedLanguages.Default;

        var user = new User
        {
            Username = username,
            Email = email,
            PasswordHash = passwordHasher.Hash(request.Password),
            FullName = Normalize.FullName(request.FullName),
            PhoneNumber = Normalize.PhoneNumber(request.PhoneNumber),
            Gender = request.Gender,
            Role = request.Role,
            PreferredLanguage = language,
            EmailConfirmed = true
        };

        database.Users.Add(user);

        // An owner needs the profile row from the start: withdrawals read AvailableBalance
        // and a missing row would be indistinguishable from a zero balance.
        if (user.Role == UserRole.Owner)
        {
            database.OwnerProfiles.Add(new OwnerProfile
            {
                UserId = user.Id,
                BusinessType = BusinessType.Individual,
                AvailableBalance = 0m
            });
        }

        var session = sessionIssuer.Issue(user);

        await database.SaveChangesAsync(cancellationToken);

        // Consumed: the window must not be reusable for a second account.
        await verifiedEmails.ClearAsync(email, cancellationToken);

        return session.Tokens;
    }
}
