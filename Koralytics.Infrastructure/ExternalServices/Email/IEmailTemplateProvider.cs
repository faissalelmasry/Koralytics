namespace Koralytics.Infrastructure.ExternalServices.Email;

public interface IEmailTemplateProvider
{
    Task<(string Subject, string HtmlBody)> RenderAsync(
        string templateName,
        Dictionary<string, string> placeholders,
        CancellationToken ct = default);
}
