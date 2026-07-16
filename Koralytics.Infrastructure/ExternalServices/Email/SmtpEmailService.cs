using Koralytics.Application.DTOs.Email;
using Koralytics.Application.Interfaces.Email;
using Koralytics.Application.Options;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Koralytics.Infrastructure.ExternalServices.Email;

public class SmtpEmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly IEmailTemplateProvider _templateProvider;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(
        IOptions<EmailSettings> settings,
        IEmailTemplateProvider templateProvider,
        ILogger<SmtpEmailService> logger)
    {
        _settings = settings.Value;
        _templateProvider = templateProvider;
        _logger = logger;
    }

    public async Task SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        try
        {
            var mimeMessage = new MimeMessage();
            mimeMessage.From.Add(new MailboxAddress(_settings.SenderName, _settings.SenderEmail));

            foreach (var to in message.To)
                mimeMessage.To.Add(MailboxAddress.Parse(to));

            foreach (var cc in message.Cc)
                mimeMessage.Cc.Add(MailboxAddress.Parse(cc));

            foreach (var bcc in message.Bcc)
                mimeMessage.Bcc.Add(MailboxAddress.Parse(bcc));

            mimeMessage.Subject = message.Subject;

            var builder = new BodyBuilder
            {
                HtmlBody = message.HtmlBody,
                TextBody = message.PlainTextBody
            };

            foreach (var attachment in message.Attachments)
            {
                builder.Attachments.Add(attachment.FileName, attachment.Content, ContentType.Parse(attachment.ContentType));
            }

            mimeMessage.Body = builder.ToMessageBody();

            using var client = new SmtpClient();
            client.Timeout = _settings.TimeoutMs;

            var secureSocketOptions = _settings.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto;

            await client.ConnectAsync(_settings.SmtpHost, _settings.SmtpPort, secureSocketOptions, ct);

            if (!string.IsNullOrEmpty(_settings.Username) && !string.IsNullOrEmpty(_settings.Password))
            {
                await client.AuthenticateAsync(_settings.Username, _settings.Password, ct);
            }

            await client.SendAsync(mimeMessage, ct);
            await client.DisconnectAsync(true, ct);

            _logger.LogInformation("Successfully sent email with subject '{Subject}' to {Count} recipients.", message.Subject, message.To.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email with subject '{Subject}' to recipients: {Recipients}", message.Subject, string.Join(", ", message.To));
            throw; // Caller decides how to handle failure
        }
    }

    public async Task SendTemplatedAsync(string toEmail, string templateName, Dictionary<string, string> placeholders, CancellationToken ct = default)
    {
        var (subject, htmlBody) = await _templateProvider.RenderAsync(templateName, placeholders, ct);

        var message = new EmailMessage
        {
            To = [toEmail],
            Subject = subject,
            HtmlBody = htmlBody
        };

        await SendAsync(message, ct);
    }

    public Task SendAccountConfirmationAsync(string toEmail, string userName, string confirmationLink, CancellationToken ct = default)
    {
        return SendTemplatedAsync(toEmail, "AccountConfirmation", new Dictionary<string, string>
        {
            { "UserName", userName },
            { "ConfirmationLink", confirmationLink }
        }, ct);
    }

    public Task SendOtpAsync(string toEmail, string userName, string otpCode, int expiryMinutes, CancellationToken ct = default)
    {
        return SendTemplatedAsync(toEmail, "OtpVerification", new Dictionary<string, string>
        {
            { "UserName", userName },
            { "OtpCode", otpCode },
            { "ExpiryMinutes", expiryMinutes.ToString() }
        }, ct);
    }

    public Task SendPasswordResetAsync(string toEmail, string userName, string resetLink, CancellationToken ct = default)
    {
        return SendTemplatedAsync(toEmail, "PasswordReset", new Dictionary<string, string>
        {
            { "UserName", userName },
            { "ResetLink", resetLink }
        }, ct);
    }

    public Task SendWelcomeAsync(string toEmail, string userName, CancellationToken ct = default)
    {
        return SendTemplatedAsync(toEmail, "Welcome", new Dictionary<string, string>
        {
            { "UserName", userName }
        }, ct);
    }

    public Task SendNotificationAsync(string toEmail, string subject, string messageBody, CancellationToken ct = default)
    {
        return SendTemplatedAsync(toEmail, "GeneralNotification", new Dictionary<string, string>
        {
            { "Subject", subject },
            { "MessageBody", messageBody }
        }, ct);
    }
}
