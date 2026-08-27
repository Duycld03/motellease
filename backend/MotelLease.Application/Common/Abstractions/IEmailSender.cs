namespace MotelLease.Application.Common.Abstractions;

/// <summary>
/// Resolves a resource key for a language. Handlers never build a user-facing sentence
/// themselves — the key is the contract, the text is data (CLAUDE.md, Language).
/// </summary>
public interface ILocalizer
{
    string Get(string key, string language, params object[] arguments);
}

public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}

/// <summary>Subject and body are already rendered; the caller owns localization.</summary>
public sealed record EmailMessage(string ToEmail, string Subject, string HtmlBody);
