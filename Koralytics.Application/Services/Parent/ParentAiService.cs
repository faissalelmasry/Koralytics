using Koralytics.Application.DTOs.Parent;
using Koralytics.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Koralytics.Application.Services.Parent
{
    public class ParentAiService : IParentAiService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ParentAiService> _logger;

        private readonly HashSet<string> _sqlToolNames;
        private readonly HashSet<string> _ragToolNames;

        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public ParentAiService(HttpClient httpClient, IConfiguration configuration, ILogger<ParentAiService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;

            _sqlToolNames = ParseConfigList(configuration["Langflow:SqlToolNames"], "sql_select_component");
            _ragToolNames = ParseConfigList(configuration["Langflow:RagToolNames"], "mongodb_atlas_search,knowledge_base_search,search_documents");
        }

        private static HashSet<string> ParseConfigList(string? raw, string fallback)
        {
            var source = string.IsNullOrWhiteSpace(raw) ? fallback : raw;
            return source
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => s.ToLowerInvariant())
                .ToHashSet();
        }

        public async Task<ParentChatResponse> ProcessParentQueryAsync(
            ParentChatRequest request,
            string parentUserId,
            IReadOnlyList<int> authorizedPlayerIds,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var (url, payload) = BuildRunRequest(request, parentUserId, authorizedPlayerIds, stream: false);

                ConfigureHeaders();

                var response = await _httpClient.PostAsJsonAsync(url, payload, cancellationToken);
                var raw = await response.Content.ReadAsStringAsync(cancellationToken);

                _logger.LogDebug("Langflow response status {StatusCode}", (int)response.StatusCode);
                _logger.LogDebug("Langflow raw response: {Raw}", raw);

                response.EnsureSuccessStatusCode();

                using var json = JsonDocument.Parse(raw);

                var answer = ExtractAnswerText(json) ?? "لم يتم إنشاء رد.";
                var (usedSql, usedRag) = DetectToolUsage(json);

                stopwatch.Stop();

                return new ParentChatResponse
                {
                    SessionId = request.SessionId,
                    Question = request.Message,
                    Answer = answer,
                    UsedSQL = usedSql,
                    UsedRAG = usedRag,
                    ResponseTime = stopwatch.Elapsed.TotalSeconds,
                    Timestamp = DateTime.UtcNow,
                    Success = true
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex, "Error calling Langflow for parent {ParentUserId}, session {SessionId}",
                    parentUserId, request.SessionId);

                return new ParentChatResponse
                {
                    SessionId = request.SessionId,
                    Question = request.Message,
                    Answer = "عذراً، حدث خطأ أثناء الوصول إلى بيانات الأكاديمية. يرجى المحاولة مرة أخرى لاحقاً.",
                    UsedSQL = false,
                    UsedRAG = false,
                    ResponseTime = stopwatch.Elapsed.TotalSeconds,
                    Timestamp = DateTime.UtcNow,
                    Success = false,
                    InternalErrorDetail = ex.ToString()
                };
            }
        }

        public async IAsyncEnumerable<string> StreamParentQueryAsync(
            ParentChatRequest request,
            string parentUserId,
            IReadOnlyList<int> authorizedPlayerIds,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var (url, payload) = BuildRunRequest(request, parentUserId, authorizedPlayerIds, stream: true);

            ConfigureHeaders();

            using var requestMessage = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = JsonContent.Create(payload)
            };

            using var response = await _httpClient.SendAsync(
                requestMessage, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new StreamReader(stream);

            var fullAnswer = new System.Text.StringBuilder();
            bool usedSql = false;
            bool usedRag = false;

            while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(cancellationToken);
                if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data:"))
                {
                    continue;
                }

                var jsonPart = line["data:".Length..].Trim();
                if (string.IsNullOrWhiteSpace(jsonPart))
                {
                    continue;
                }

                JsonDocument? eventDoc;
                try
                {
                    eventDoc = JsonDocument.Parse(jsonPart);
                }
                catch (JsonException)
                {
                    continue;
                }

                using (eventDoc)
                {
                    var tokenText = TryExtractStreamToken(eventDoc.RootElement);
                    if (tokenText != null)
                    {
                        fullAnswer.Append(tokenText);

                        var chunkPayload = JsonSerializer.Serialize(new ParentChatStreamChunk
                        {
                            EventType = "token",
                            Text = tokenText
                        }, _jsonOptions);

                        yield return chunkPayload;
                        continue;
                    }

                    if (eventDoc.RootElement.TryGetProperty("outputs", out _))
                    {
                        var (sql, rag) = DetectToolUsage(eventDoc);
                        usedSql = sql;
                        usedRag = rag;

                        var finalAnswer = ExtractAnswerText(eventDoc) ?? fullAnswer.ToString();
                        stopwatch.Stop();

                        var metaPayload = JsonSerializer.Serialize(new ParentChatStreamChunk
                        {
                            EventType = "meta",
                            Meta = new ParentChatResponse
                            {
                                SessionId = request.SessionId,
                                Question = request.Message,
                                Answer = finalAnswer,
                                UsedSQL = usedSql,
                                UsedRAG = usedRag,
                                ResponseTime = stopwatch.Elapsed.TotalSeconds,
                                Timestamp = DateTime.UtcNow,
                                Success = true
                            }
                        }, _jsonOptions);

                        yield return metaPayload;
                    }
                }
            }
        }

        private void ConfigureHeaders()
        {
            var apiKey = _configuration["Langflow:ParentApiKey"];

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                _httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
            }
            else
            {
                _logger.LogWarning("Langflow:ParentApiKey is missing or empty in configuration.");
            }
        }

        private (string url, object payload) BuildRunRequest(
            ParentChatRequest request, string parentUserId, IReadOnlyList<int> authorizedPlayerIds, bool stream)
        {
            // TrimEnd('/') بيمنع مشكلة الـ double slash لو appsettings فيها trailing slash
            var baseUrl = _configuration["Langflow:BaseUrl"]?.TrimEnd('/');
            var flowId = _configuration["Langflow:ParentFlowId"];

            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                _logger.LogError("Langflow:BaseUrl is missing or empty in configuration.");
                throw new InvalidOperationException("Langflow:BaseUrl is not configured.");
            }

            if (string.IsNullOrWhiteSpace(flowId))
            {
                _logger.LogError("Langflow:ParentFlowId is missing or empty in configuration.");
                throw new InvalidOperationException("Langflow:ParentFlowId is not configured.");
            }

            var idsString = string.Join(", ", authorizedPlayerIds);

            var prompt = $"""
        Authorized Player IDs: {idsString}

        User Question:
        {request.Message}
        """;

            var secureSessionId = $"parent_{parentUserId}";

            var payloadDict = new Dictionary<string, object?>
            {
                ["input_value"] = prompt,
                ["input_type"] = "chat",
                ["output_type"] = "chat",
                ["session_id"] = secureSessionId
            };

            var tweakComponentId = _configuration["Langflow:PlayerIdTweakComponentId"];
            var tweakFieldName = _configuration["Langflow:PlayerIdTweakFieldName"];

            if (!string.IsNullOrWhiteSpace(tweakComponentId) && !string.IsNullOrWhiteSpace(tweakFieldName))
            {
                payloadDict["tweaks"] = new Dictionary<string, object>
                {
                    [tweakComponentId] = new Dictionary<string, object>
                    {
                        [tweakFieldName] = idsString
                    }
                };
            }

            var url = $"{baseUrl}/api/v1/run/{flowId}?stream={(stream ? "true" : "false")}";
            return (url, payloadDict);
        }
        private static string? ExtractAnswerText(JsonDocument json)
        {
            try
            {
                return json.RootElement
                    .GetProperty("outputs")[0]
                    .GetProperty("outputs")[0]
                    .GetProperty("results")
                    .GetProperty("message")
                    .GetProperty("text")
                    .GetString();
            }
            catch (Exception)
            {
                return null;
            }
        }

        private (bool usedSql, bool usedRag) DetectToolUsage(JsonDocument json)
        {
            bool usedSql = false;
            bool usedRag = false;

            void Walk(JsonElement element)
            {
                if (usedSql && usedRag) return;

                switch (element.ValueKind)
                {
                    case JsonValueKind.Object:
                        if (element.TryGetProperty("type", out var typeProp) &&
                            typeProp.ValueKind == JsonValueKind.String &&
                            string.Equals(typeProp.GetString(), "tool", StringComparison.OrdinalIgnoreCase) &&
                            element.TryGetProperty("name", out var nameProp) &&
                            nameProp.ValueKind == JsonValueKind.String)
                        {
                            var toolName = nameProp.GetString()?.ToLowerInvariant() ?? string.Empty;

                            var isSuccess = !element.TryGetProperty("status", out var statusProp)
                                             || statusProp.ValueKind != JsonValueKind.String
                                             || string.Equals(statusProp.GetString(), "success", StringComparison.OrdinalIgnoreCase);

                            if (isSuccess)
                            {
                                if (_sqlToolNames.Contains(toolName)) usedSql = true;
                                if (_ragToolNames.Contains(toolName)) usedRag = true;
                            }
                        }

                        foreach (var prop in element.EnumerateObject())
                        {
                            Walk(prop.Value);
                        }
                        break;

                    case JsonValueKind.Array:
                        foreach (var item in element.EnumerateArray())
                        {
                            Walk(item);
                        }
                        break;
                }
            }

            Walk(json.RootElement);
            return (usedSql, usedRag);
        }

        private static string? TryExtractStreamToken(JsonElement root)
        {
            if (root.TryGetProperty("data", out var dataEl) &&
                dataEl.ValueKind == JsonValueKind.Object &&
                dataEl.TryGetProperty("chunk", out var chunkEl) &&
                chunkEl.ValueKind == JsonValueKind.String)
            {
                return chunkEl.GetString();
            }

            if (root.TryGetProperty("chunk", out var directChunkEl) &&
                directChunkEl.ValueKind == JsonValueKind.String)
            {
                return directChunkEl.GetString();
            }

            return null;
        }
    }
}