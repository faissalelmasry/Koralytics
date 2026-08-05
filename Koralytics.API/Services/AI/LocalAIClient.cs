using Koralytics.Application.Interfaces.AI;
using Microsoft.Extensions.Configuration;
using System.Text;
using System.Text.Json;

namespace Koralytics.API.Services.AI
{
    /// <summary>
    /// Calls the Google Gemini API to generate a real, tournament-specific AI report.
    /// Uses the free generativelanguage.googleapis.com endpoint — no billing required
    /// for Google AI Studio keys.
    ///
    /// Set your key via User Secrets (never hardcode it):
    ///   dotnet user-secrets set "AI:GoogleApiKey" "YOUR_KEY_HERE"
    /// </summary>
    public class LocalAIClient : IAIProvider
    {
        // ─── CONFIGURATION ──────────────────────────────────────────
        // API key is read from User Secrets / env vars — NEVER hardcoded.
        private readonly string _apiKey;

        // Model options (all free on Google AI Studio):
        //   "gemini-2.5-flash"   ← recommended
        //   "gemini-2.0-flash"   ← also supported
        private const string Model = "gemini-2.5-flash";
        private const int MaxOutputTokens = 8192;
        // ────────────────────────────────────────────────────────────

        private static readonly HttpClient _http = new HttpClient();

        public LocalAIClient(IConfiguration configuration)
        {
            _apiKey = configuration["AI:GoogleApiKey"]
                      ?? throw new InvalidOperationException(
                          "AI:GoogleApiKey is not configured. " +
                          "Run: dotnet user-secrets set \"AI:GoogleApiKey\" \"YOUR_KEY\"");
        }

        public async Task<string> GenerateTournamentReportAsync(
            string prompt,
            CancellationToken cancellationToken = default)
        {
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{Model}:generateContent?key={_apiKey}";

            var body = new
            {
                system_instruction = new
                {
                    parts = new[]
                    {
                        new
                        {
                            text =
                                "أنت محلل كرة قدم متخصص في الأكاديميات الرياضية. " +
                                "تكتب تقارير فنية وتكتيكية احترافية باللغة العربية. " +
                                "يجب أن يكون تقريرك دقيقاً بناءً على البيانات المقدمة، " +
                                "ومُنظَّماً بعناوين وتحليل موضوعي مفيد لمديري الأكاديميات. " +
                                "لا تختلق بيانات غير موجودة في المدخلات."
                        }
                    }
                },
                contents = new[]
                {
                    new
                    {
                        role = "user",
                        parts = new[] { new { text = prompt } }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.75,
                    maxOutputTokens = MaxOutputTokens,
                    topP = 0.9
                }
            };

            var json = JsonSerializer.Serialize(body);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var response = await _http.PostAsync(url, content, cancellationToken);

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            response.EnsureSuccessStatusCode();

            var parsed = JsonSerializer.Deserialize<GeminiResponse>(
                responseJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var text = parsed?.Candidates?[0]?.Content?.Parts?[0]?.Text;
            return text?.Trim() ?? string.Empty;
        }

        // ── Gemini response model ────────────────────────────────────
        private class GeminiResponse { public GeminiCandidate[]? Candidates { get; set; } }
        private class GeminiCandidate { public GeminiContent? Content { get; set; } }
        private class GeminiContent { public GeminiPart[]? Parts { get; set; } }
        private class GeminiPart { public string? Text { get; set; } }
    }
}
