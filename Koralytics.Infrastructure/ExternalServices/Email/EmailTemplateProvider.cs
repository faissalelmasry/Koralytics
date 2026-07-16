using System.Collections.Concurrent;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace Koralytics.Infrastructure.ExternalServices.Email;

public class EmailTemplateProvider : IEmailTemplateProvider
{
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<EmailTemplateProvider> _logger;
    private readonly ConcurrentDictionary<string, string> _templateCache = new();

    public EmailTemplateProvider(IWebHostEnvironment env, ILogger<EmailTemplateProvider> logger)
    {
        _env = env;
        _logger = logger;
    }

    public async Task<(string Subject, string HtmlBody)> RenderAsync(
        string templateName, 
        Dictionary<string, string> placeholders, 
        CancellationToken ct = default)
    {
        string templateContent = await GetTemplateContentAsync(templateName, ct);
        
        // Extract subject from HTML comment: <!-- Subject: Welcome to Koralytics -->
        var subjectMatch = Regex.Match(templateContent, @"<!--\s*Subject:\s*(.+?)\s*-->", RegexOptions.IgnoreCase);
        string subject = subjectMatch.Success ? subjectMatch.Groups[1].Value : "Koralytics Notification";

        // Replace placeholders
        string body = templateContent;
        foreach (var kvp in placeholders)
        {
            body = body.Replace($"{{{{{kvp.Key}}}}}", kvp.Value);
        }

        // Auto-inject common variables if missing
        if (!placeholders.ContainsKey("Year"))
        {
            body = body.Replace("{{Year}}", DateTime.UtcNow.Year.ToString());
        }

        return (subject, body);
    }

    private async Task<string> GetTemplateContentAsync(string templateName, CancellationToken ct)
    {
        if (_templateCache.TryGetValue(templateName, out var cachedTemplate))
        {
            return cachedTemplate;
        }

        var assembly = Assembly.GetEntryAssembly();
        var resourceName = $"{assembly!.GetName().Name}.Templates.{templateName}.html";

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            _logger.LogError("Email template not found at resource: {ResourceName}", resourceName);
            throw new FileNotFoundException($"Email template '{templateName}' not found as embedded resource.");
        }

        using var reader = new StreamReader(stream);
        var content = await reader.ReadToEndAsync(ct);

        _templateCache.TryAdd(templateName, content);

        return content;
    }
}
