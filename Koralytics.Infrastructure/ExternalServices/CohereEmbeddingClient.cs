using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Koralytics.Application.Interfaces;
using Koralytics.Infrastructure.Configuration;

namespace Koralytics.Infrastructure.ExternalServices
{
    public class CohereEmbeddingClient : ICohereEmbeddingClient
    {
        private readonly HttpClient _http;
        private readonly CohereOptions _options;

        public CohereEmbeddingClient(HttpClient http, IOptions<CohereOptions> options)
        {
            _http = http;
            _options = options.Value;

            if (!string.IsNullOrWhiteSpace(_options.BaseUrl))
            {
                var baseUrl = _options.BaseUrl.TrimEnd('/') + "/";
                _http.BaseAddress = new Uri(baseUrl);
            }
            if (!string.IsNullOrWhiteSpace(_options.ApiKey))
            {
                _http.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", _options.ApiKey);
            }
        }

        public async Task<float[]> GetEmbeddingAsync(string text, CancellationToken ct = default)
        {
            var payload = new
            {
                texts = new[] { text },
                model = _options.Model,
                input_type = "search_document"
            };

            var response = await _http.PostAsJsonAsync("embed", payload, ct);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<CohereEmbedResponse>(cancellationToken: ct);
            return result?.Embeddings?.FirstOrDefault() ?? Array.Empty<float>();
        }

        private class CohereEmbedResponse
        {
            public List<float[]>? Embeddings { get; set; }
        }
    }
}
