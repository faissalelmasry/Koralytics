using Koralytics.Application.DTOs.Email;

namespace Koralytics.Application.Interfaces.Email;

public interface IEmailService
{
    // ── Low-level: send an already-built EmailMessage ──
    Task SendAsync(EmailMessage message, CancellationToken ct = default);

    // ── Convenience: send a templated email ──
    Task SendTemplatedAsync(
        string toEmail,
        string templateName,
        Dictionary<string, string> placeholders,
        CancellationToken ct = default);

    // ── Shorthand helpers for common use-cases ──
    Task SendAccountConfirmationAsync(string toEmail, string userName, string confirmationLink, CancellationToken ct = default);
    Task SendOtpAsync(string toEmail, string userName, string otpCode, int expiryMinutes, CancellationToken ct = default);
    Task SendPasswordResetAsync(string toEmail, string userName, string resetLink, CancellationToken ct = default);
    Task SendWelcomeAsync(string toEmail, string userName, CancellationToken ct = default);
    Task SendNotificationAsync(string toEmail, string subject, string messageBody, CancellationToken ct = default);
}
