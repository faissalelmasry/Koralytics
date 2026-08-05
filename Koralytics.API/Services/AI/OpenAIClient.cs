using Koralytics.Application.Options;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Koralytics.Application.Interfaces.AI;

namespace Koralytics.API.Services.AI
{
    public class OpenAIClient : IAIProvider
    {
        private readonly HttpClient _httpClient;
        private readonly AIOptions _options;

        public OpenAIClient(HttpClient httpClient, IOptions<AIOptions> options)
        {
            _httpClient = httpClient;
            _options = options.Value;
        }

        public async Task<string> GenerateTournamentReportAsync(string prompt, CancellationToken cancellationToken = default)
        {
            var request = new
            {
                model = _options.Model,
                messages = new[]
                {
                    new { role = "system", content = "You are a football tournament intelligence assistant that writes concise executive summaries for academy administrators." },
                    new { role = "user", content = prompt }
                },
                max_tokens = _options.MaxTokens,
                temperature = 0.7
            };

            var json = JsonSerializer.Serialize(request);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var response = await _httpClient.PostAsync("/chat/completions", content, cancellationToken);
            var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
            response.EnsureSuccessStatusCode();

            var parsed = JsonSerializer.Deserialize<OpenAIChatResponse>(responseText,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return parsed?.Choices?.FirstOrDefault()?.Message?.Content?.Trim() ?? string.Empty;
        }

        private class OpenAIChatResponse
        {
            public List<OpenAIChoice>? Choices { get; set; }
        }

        private class OpenAIChoice
        {
            public OpenAIMessage? Message { get; set; }
        }

        private class OpenAIMessage
        {
            public string? Content { get; set; }
        }
    }
}
