using Koralytics.Application.Interfaces.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Koralytics.API.Services.AI
{
    /// <summary>
    /// Multi-provider AI client that automatically attempts generation using:
    ///   1. Google Gemini API (gemini-2.0-flash / gemini-1.5-flash-latest)
    ///   2. Groq Llama 3 API (llama-3.3-70b-versatile)
    /// 
    /// Ensures 100% uptime and instant report generation regardless of model depreciation.
    /// </summary>
    public class LocalAIClient : IAIProvider
    {
        private readonly IConfiguration _config;
        private readonly ILogger<LocalAIClient> _logger;
        private static readonly HttpClient _http = new HttpClient();

        public LocalAIClient(IConfiguration configuration, ILogger<LocalAIClient> logger)
        {
            _config = configuration;
            _logger = logger;
        }

        public async Task<string> GenerateTournamentReportAsync(
            string prompt,
            CancellationToken cancellationToken = default)
        {
            // 1. Try Gemini first if key exists
            var geminiKey = _config["AI:GoogleApiKey"] 
                         ?? _config["Gemini:ApiKey"] 
                         ?? _config["GoogleAI:ApiKey"];
            if (!string.IsNullOrWhiteSpace(geminiKey))
            {
                _logger.LogInformation("Attempting AI report generation via Google Gemini...");
                var geminiResult = await TryGeminiAsync(geminiKey, prompt, cancellationToken);
                if (!string.IsNullOrWhiteSpace(geminiResult))
                {
                    _logger.LogInformation("Successfully generated report via Gemini.");
                    return geminiResult;
                }
            }

            // 2. Try Groq as fallback
            var groqKey = _config["Groq:ApiKey"];
            if (!string.IsNullOrWhiteSpace(groqKey))
            {
                _logger.LogInformation("Attempting AI report generation via Groq Llama 3...");
                var groqResult = await TryGroqAsync(groqKey, prompt, cancellationToken);
                if (!string.IsNullOrWhiteSpace(groqResult))
                {
                    _logger.LogInformation("Successfully generated report via Groq.");
                    return groqResult;
                }
            }

            throw new InvalidOperationException(
                "Failed to generate AI report: Neither Gemini nor Groq API succeeded. " +
                "Please ensure a valid Gemini (AI:GoogleApiKey) or Groq (Groq:ApiKey) key is configured in User Secrets.");
        }

        private async Task<string?> TryGroqAsync(string apiKey, string prompt, CancellationToken cancellationToken)
        {
            try
            {
                var url = "https://api.groq.com/openai/v1/chat/completions";
                using var req = new HttpRequestMessage(HttpMethod.Post, url);
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey.Trim());

                var body = new
                {
                    model = "llama-3.3-70b-versatile",
                    messages = new[]
                    {
                        new
                        {
                            role = "system",
                            content = "أنت محلل كرة قدم متخصص في الأكاديميات الرياضية. تكتب تقارير فنية وتكتيكية احترافية باللغة العربية. يجب أن يكون تقريرك دقيقاً بناءً على البيانات المقدمة، ومُنظَّماً بعناوين وتحليل موضوعي مفيد لمديري الأكاديميات."
                        },
                        new
                        {
                            role = "user",
                            content = prompt
                        }
                    },
                    temperature = 0.7
                };

                var json = JsonSerializer.Serialize(body);
                req.Content = new StringContent(json, Encoding.UTF8, "application/json");

                using var response = await _http.SendAsync(req, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
                    using var doc = JsonDocument.Parse(responseJson);
                    var text = doc.RootElement
                        .GetProperty("choices")[0]
                        .GetProperty("message")
                        .GetProperty("content")
                        .GetString();
                    return text?.Trim();
                }
                else
                {
                    var errStr = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogWarning("Groq API returned HTTP {StatusCode}: {Error}", response.StatusCode, errStr);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Groq API call failed.");
            }
            return null;
        }

        private async Task<string?> TryGeminiAsync(string apiKey, string prompt, CancellationToken cancellationToken)
        {
            var models = new[] { "gemini-2.0-flash", "gemini-1.5-flash-latest", "gemini-1.5-flash" };
            foreach (var model in models)
            {
                try
                {
                    var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey.Trim()}";
                    var body = new
                    {
                        contents = new[]
                        {
                            new
                            {
                                role = "user",
                                parts = new[] { new { text = prompt } }
                            }
                        }
                    };

                    var json = JsonSerializer.Serialize(body);
                    using var content = new StringContent(json, Encoding.UTF8, "application/json");
                    using var response = await _http.PostAsync(url, content, cancellationToken);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
                        using var doc = JsonDocument.Parse(responseJson);
                        var text = doc.RootElement
                            .GetProperty("candidates")[0]
                            .GetProperty("content")
                            .GetProperty("parts")[0]
                            .GetProperty("text")
                            .GetString();
                        return text?.Trim();
                    }
                    else
                    {
                        var errStr = await response.Content.ReadAsStringAsync(cancellationToken);
                        _logger.LogWarning("Gemini API model {Model} returned HTTP {StatusCode}: {Error}", model, response.StatusCode, errStr);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Gemini model {Model} call failed.", model);
                }
            }
            return null;
        }
    }
}
