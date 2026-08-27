using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using MotelLease.Application.Common.Abstractions;

namespace MotelLease.Infrastructure.Email;

public sealed class SmtpEmailSender(
    IOptions<SmtpOptions> options,
    ILogger<SmtpEmailSender> logger) : IEmailSender
{
    private readonly SmtpOptions _options = options.Value;

    /// <summary>
    /// Registered only when SmtpOptions.IsConfigured, so Host is present; the local keeps the
    /// compiler from having to take that on trust.
    /// </summary>
    private string Host => _options.Host
        ?? throw new InvalidOperationException("Smtp:Host is missing.");

    public async Task SendAsync(
        EmailMessage message,
        CancellationToken cancellationToken = default)
    {
        var mail = new MimeMessage
        {
            Subject = message.Subject,
            Body = new BodyBuilder { HtmlBody = message.HtmlBody }.ToMessageBody()
        };

        mail.From.Add(new MailboxAddress(_options.FromName, _options.FromAddress));
        mail.To.Add(MailboxAddress.Parse(message.ToEmail));

        using var client = new SmtpClient();

        try
        {
            await client.ConnectAsync(
                Host,
                _options.Port,
                _options.UseStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.None,
                cancellationToken);

            if (!string.IsNullOrWhiteSpace(_options.Username))
            {
                await client.AuthenticateAsync(
                    _options.Username, _options.Password ?? string.Empty, cancellationToken);
            }

            await client.SendAsync(mail, cancellationToken);
        }
        finally
        {
            if (client.IsConnected)
            {
                await client.DisconnectAsync(true, cancellationToken);
            }
        }

        logger.LogInformation("Sent {Subject} to {Recipient}.", message.Subject, message.ToEmail);
    }
}

/// <summary>
/// Development fallback when no SMTP host is configured: the message, code included, goes to
/// the log so the OTP flows are testable without a mail server. Refuses to be used outside
/// development — see DependencyInjection.
/// </summary>
public sealed class LoggingEmailSender(ILogger<LoggingEmailSender> logger) : IEmailSender
{
    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        logger.LogWarning(
            "SMTP is not configured. Email to {Recipient} was not sent.\nSubject: {Subject}\n{Body}",
            message.ToEmail,
            message.Subject,
            message.HtmlBody);

        return Task.CompletedTask;
    }
}
