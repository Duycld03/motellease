using Microsoft.EntityFrameworkCore;
using MotelLease.Application.Auth.Contracts;
using MotelLease.Application.Common;
using MotelLease.Application.Common.Abstractions;
using MotelLease.Application.Common.Errors;
using MotelLease.Application.Common.Security;

namespace MotelLease.Application.Auth;

/// <summary>POST /auth/login. Accepts a username or an email in the same field.</summary>
public sealed class LoginHandler(
    IAppDbContext database,
    IPasswordHasher passwordHasher,
    SessionIssuer sessionIssuer)
{
    public async Task<AuthTokensResponse> HandleAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var login = Normalize.Email(request.Login);

        var user = await database.Users
            .FirstOrDefaultAsync(u => u.Email == login || u.Username == login, cancellationToken);

        // One message for "no such account", "wrong password" and "Google-only account":
        // telling them apart hands an attacker a list of valid addresses.
        if (user is null
            || UnusablePassword.IsUnusable(user.PasswordHash)
            || !passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new AuthenticationException(MessageKeys.Auth.InvalidCredentials);
        }

        // Locked is reported plainly — the caller already proved the account is theirs, and
        // hiding it would only produce support tickets.
        if (user.IsLocked)
        {
            throw new ForbiddenException(MessageKeys.Auth.AccountLocked);
        }

        var session = sessionIssuer.Issue(user);

        await database.SaveChangesAsync(cancellationToken);

        return session.Tokens;
    }
}
