using System.Net.Http.Json;
using System.Text.Json;
using Koralytics.Application.DTOs.Analytics;
using Koralytics.Application.Interfaces.Analytics;
using Koralytics.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Koralytics.Application.Services.Analytics
{
    /// <summary>
    /// HTTP bridge between the .NET API and Ali's Langflow AI Player Search pipeline.
    /// Sends the user's natural-language query to Langflow, extracts the conversational
    /// response from the nested JSON, and returns it as a clean DTO.
    /// </summary>
    public class AnalyticsService : IAnalyticsService
    {
        private readonly HttpClient _httpClient;
        private readonly LangflowOptions _options;
        private readonly ILogger<AnalyticsService> _logger;

        public AnalyticsService(
            IHttpClientFactory httpClientFactory,
            IOptions<LangflowOptions> options,
            ILogger<AnalyticsService> logger)
        {
            _httpClient = httpClientFactory.CreateClient("Langflow");
            _options = options.Value;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<AiSearchResponseDto> AiPlayerSearchAsync(AiSearchRequestDto request)
        {
            var langflowUrl = $"/api/v1/run/{_options.FlowId}";

            var payload = new
            {
                input_value = request.Query,
                output_type = "chat",
                input_type = "chat"
            };

            _logger.LogInformation(
                "Sending AI Player Search query to Langflow. FlowId={FlowId}, Query=\"{Query}\"",
                _options.FlowId, request.Query);

            HttpResponseMessage response;
            try
            {
                response = await _httpClient.PostAsJsonAsync(langflowUrl, payload);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex,
                    "Failed to connect to Langflow at {BaseUrl}. Is the Langflow service running?",
                    _options.BaseUrl);

                throw new InvalidOperationException(
                    "The AI search service is currently unavailable. " +
                    "Please ensure the Langflow service is running and try again.", ex);
            }

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogError(
                    "Langflow returned HTTP {StatusCode}. Body: {Body}",
                    (int)response.StatusCode, errorBody);

                throw new InvalidOperationException(
                    $"The AI search service returned an error (HTTP {(int)response.StatusCode}). " +
                    "Please try again later.");
            }

            var answerText = await ExtractAnswerFromLangflowResponseAsync(response);

            _logger.LogInformation("AI Player Search completed successfully.");

            return new AiSearchResponseDto { Answer = answerText };
        }

        /// <summary>
        /// Extracts the human-readable text from Langflow's deeply nested JSON response.
        /// Expected path: outputs[0].outputs[0].results.message.text
        /// </summary>
        private async Task<string> ExtractAnswerFromLangflowResponseAsync(HttpResponseMessage response)
        {
            var json = await response.Content.ReadAsStringAsync();

            try
            {
                using var document = JsonDocument.Parse(json);
                var root = document.RootElement;

                var text = root
                    .GetProperty("outputs")[0]
                    .GetProperty("outputs")[0]
                    .GetProperty("results")
                    .GetProperty("message")
                    .GetProperty("text")
                    .GetString();

                if (string.IsNullOrWhiteSpace(text))
                {
                    _logger.LogWarning("Langflow returned an empty text response.");
                    return "The AI search returned no results. Please try rephrasing your question.";
                }

                return text;
            }
            catch (Exception ex) when (ex is JsonException or KeyNotFoundException or IndexOutOfRangeException or InvalidOperationException)
            {
                _logger.LogError(ex,
                    "Failed to parse Langflow response. Raw JSON: {Json}",
                    json.Length > 500 ? json[..500] + "..." : json);

                throw new InvalidOperationException(
                    "Received an unexpected response format from the AI search service.", ex);
            }
        }
    }
}
