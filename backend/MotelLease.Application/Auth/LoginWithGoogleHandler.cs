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
/// POST /auth/login/google. The ID token is verified server-side against Google's keys —
/// never trusted because the client says so.
/// </summary>
public sealed class LoginWithGoogleHandler(
    IAppDbContext database,
    IGoogleTokenVerifier googleTokens,
    SessionIssuer sessionIssuer)
{
    public async Task<AuthTokensResponse> HandleAsync(
        GoogleLoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var identity = await googleTokens.VerifyAsync(request.IdToken, cancellationToken)
            ?? throw new AuthenticationException(MessageKeys.Auth.GoogleTokenInvalid);

        // An unverified Google address would let someone claim an email they do not own and
        // then take over the matching password account below.
        if (!identity.EmailVerified)
        {
            throw new AuthenticationException(MessageKeys.Auth.GoogleEmailUnverified);
        }

        var email = Normalize.Email(identity.Email);

        // Match on the Google subject first: it is stable, while the address on the Google
        // side can be changed by its owner.
        var user = await database.Users
            .FirstOrDefaultAsync(u => u.SocialId == identity.Subject, cancellationToken)
            ?? await database.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        if (user is null)
        {
            user = await CreateAccountAsync(request, identity, email, cancellationToken);
        }
        else
        {
            if (user.IsLocked)
            {
                throw new ForbiddenException(MessageKeys.Auth.AccountLocked);
            }

            // First Google sign-in on an existing password account links the two. Safe only
            // because the address was verified by Google just above.
            user.SocialId ??= identity.Subject;
            user.EmailConfirmed = true;
        }

        var session = sessionIssuer.Issue(user);

        await database.SaveChangesAsync(cancellationToken);

        return session.Tokens;
    }

    private async Task<User> CreateAccountAsync(
        GoogleLoginRequest request,
        GoogleIdentity identity,
        string email,
        CancellationToken cancellationToken)
    {
        var role = request.Role ?? UserRole.Tenant;

        if (!SelfAssignableRoles.IsAllowed(role))
        {
            throw new ForbiddenException(MessageKeys.Auth.RoleNotSelfAssignable);
        }

        var user = new User
        {
            Username = await GenerateUsernameAsync(email, cancellationToken),
            Email = email,
            // No password was ever chosen; /auth/password/forgot is how one gets set later.
            PasswordHash = UnusablePassword.Create(),
            FullName = Normalize.FullName(identity.FullName ?? email.Split('@')[0]),
            Gender = Gender.Other,
            Role = role,
            SocialId = identity.Subject,
            AvatarUrl = identity.PictureUrl,
            PreferredLanguage = SupportedLanguages.Default,
            EmailConfirmed = true
        };

        database.Users.Add(user);

        if (role == UserRole.Owner)
        {
            database.OwnerProfiles.Add(new OwnerProfile
            {
                UserId = user.Id,
                BusinessType = BusinessType.Individual,
                AvailableBalance = 0m
            });
        }

        return user;
    }

    /// <summary>
    /// Google gives no username, so one is derived from the address and suffixed until it is
    /// free. Bounded so a pathological prefix cannot loop forever.
    /// </summary>
    private async Task<string> GenerateUsernameAsync(string email, CancellationToken cancellationToken)
    {
        var seed = new string(email.Split('@')[0]
            .Where(c => char.IsLetterOrDigit(c) || c is '.' or '_' or '-')
            .ToArray());

        var baseName = Normalize.Username(seed.Length >= 3 ? seed : "user" + seed);
        baseName = baseName.Length > 56 ? baseName[..56] : baseName;

        for (var suffix = 0; suffix < 100; suffix++)
        {
            var candidate = suffix == 0 ? baseName : $"{baseName}{suffix}";

            if (!await database.Users.AnyAsync(u => u.Username == candidate, cancellationToken))
            {
                return candidate;
            }
        }

        return $"{baseName}{Guid.CreateVersion7().ToString("n")[..8]}";
    }
}
