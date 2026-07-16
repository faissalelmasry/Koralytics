namespace Koralytics.Application.DTOs.Email;

public class EmailMessage
{
    // Recipients
    public List<string> To { get; set; } = [];
    public List<string> Cc { get; set; } = [];
    public List<string> Bcc { get; set; } = [];

    // Content
    public string Subject { get; set; } = string.Empty;
    public string? HtmlBody { get; set; }
    public string? PlainTextBody { get; set; }
    public bool IsHtml => !string.IsNullOrEmpty(HtmlBody);

    // Attachments
    public List<EmailAttachment> Attachments { get; set; } = [];
}
