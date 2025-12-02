using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using BitNow_Backend.BLL.IServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BitNow_Backend.BLL.Services
{
    public class PineconeService : IPineconeService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PineconeService> _logger;

        public PineconeService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<PineconeService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        private string GetApiKey()
        {
            var apiKey = _configuration["Pinecone:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException("Pinecone API key is not configured");
            }
            return apiKey;
        }

        private string GetIndexName()
        {
            return _configuration["Pinecone:IndexName"] ?? "bidnow-auctions";
        }

        private string GetBaseUrl()
        {
            var baseUrl = _configuration["Pinecone:BaseUrl"];
            if (!string.IsNullOrWhiteSpace(baseUrl))
            {
                return baseUrl;
            }

            var environment = _configuration["Pinecone:Environment"] ?? "aped-4627-b74a";
            var indexName = GetIndexName();
            var isServerless = _configuration.GetValue<bool>("Pinecone:IsServerless", true);

            if (isServerless)
            {
                return $"https://{indexName}.svc.{environment}.pinecone.io";
            }
            else
            {
                return $"https://{indexName}-{environment}.svc.pinecone.io";
            }
        }

        private HttpClient CreateClient()
        {
            var client = _httpClientFactory.CreateClient("Pinecone");
            client.BaseAddress = new Uri(GetBaseUrl());

            
            client.DefaultRequestHeaders.Add("Api-Key", GetApiKey());

            client.Timeout = TimeSpan.FromSeconds(30);
            return client;
        }

        public async Task UpsertVectorAsync(string id, float[] vector, Dictionary<string, object>? metadata = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("ID cannot be null or empty", nameof(id));
            }

            if (vector == null || vector.Length == 0)
            {
                throw new ArgumentException("Vector cannot be null or empty", nameof(vector));
            }

            try
            {
                var client = CreateClient();

                var vectorObj = new
                {
                    id = id,
                    values = vector,
                    metadata = metadata ?? new Dictionary<string, object>()
                };

                var payload = new
                {
                    vectors = new[] { vectorObj }
                };

                var json = JsonSerializer.Serialize(payload);
                using var request = new HttpRequestMessage(HttpMethod.Post, "/vectors/upsert")
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };

                using var response = await client.SendAsync(request, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorText = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError("Pinecone upsert returned non-success status {Status}: {Body}", response.StatusCode, errorText);
                    throw new InvalidOperationException($"Pinecone upsert error: {response.StatusCode} - {errorText}");
                }

                _logger.LogDebug("Successfully upserted vector with ID: {Id}", id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error upserting vector with ID: {Id}", id);
                throw;
            }
        }

        public async Task<List<(string id, float score)>> QuerySimilarAsync(float[] queryVector, int topK = 4, Dictionary<string, object>? filter = null, CancellationToken cancellationToken = default)
        {
            if (queryVector == null || queryVector.Length == 0)
            {
                throw new ArgumentException("Query vector cannot be null or empty", nameof(queryVector));
            }

            try
            {
                var client = CreateClient();

                var payload = new
                {
                    vector = queryVector,
                    topK = topK,
                    includeMetadata = true,
                    filter = filter
                };

                var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
                {
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });

                using var request = new HttpRequestMessage(HttpMethod.Post, "/query")
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };

                using var response = await client.SendAsync(request, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorText = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError("Pinecone query returned non-success status {Status}: {Body}", response.StatusCode, errorText);
                    throw new InvalidOperationException($"Pinecone query error: {response.StatusCode} - {errorText}");
                }

                using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var document = await JsonDocument.ParseAsync(contentStream, cancellationToken: cancellationToken);

                var root = document.RootElement;
                var matches = root.GetProperty("matches");

                var results = new List<(string id, float score)>();
                foreach (var match in matches.EnumerateArray())
                {
                    var matchId = match.GetProperty("id").GetString();
                    var score = (float)match.GetProperty("score").GetDouble();

                    if (!string.IsNullOrWhiteSpace(matchId))
                    {
                        results.Add((matchId, score));
                    }
                }

                _logger.LogDebug("Query returned {Count} results", results.Count);
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error querying similar vectors");
                throw;
            }
        }

        public async Task DeleteVectorAsync(string id, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("ID cannot be null or empty", nameof(id));
            }

            try
            {
                var client = CreateClient();

                var payload = new
                {
                    ids = new[] { id }
                };

                var json = JsonSerializer.Serialize(payload);
                using var request = new HttpRequestMessage(HttpMethod.Post, "/vectors/delete")
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };

                using var response = await client.SendAsync(request, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorText = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError("Pinecone delete returned non-success status {Status}: {Body}", response.StatusCode, errorText);
                    throw new InvalidOperationException($"Pinecone delete error: {response.StatusCode} - {errorText}");
                }

                _logger.LogDebug("Successfully deleted vector with ID: {Id}", id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting vector with ID: {Id}", id);
                throw;
            }
        }

        public async Task DeleteVectorsAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default)
        {
            if (ids == null || !ids.Any())
            {
                return;
            }

            try
            {
                var client = CreateClient();

                var payload = new
                {
                    ids = ids.ToArray()
                };

                var json = JsonSerializer.Serialize(payload);
                using var request = new HttpRequestMessage(HttpMethod.Post, "/vectors/delete")
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };

                using var response = await client.SendAsync(request, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorText = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError("Pinecone delete returned non-success status {Status}: {Body}", response.StatusCode, errorText);
                    throw new InvalidOperationException($"Pinecone delete error: {response.StatusCode} - {errorText}");
                }

                _logger.LogDebug("Successfully deleted {Count} vectors", ids.Count());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting vectors");
                throw;
            }
        }

        public async Task DeleteAllVectorsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var client = CreateClient();

                // Pinecone API cho phép xóa tất cả vectors bằng cách gửi deleteAll: true
                var payload = new
                {
                    deleteAll = true
                };

                var json = JsonSerializer.Serialize(payload);
                using var request = new HttpRequestMessage(HttpMethod.Post, "/vectors/delete")
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };

                using var response = await client.SendAsync(request, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorText = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError("Pinecone deleteAll returned non-success status {Status}: {Body}",
                        response.StatusCode, errorText);
                    throw new InvalidOperationException($"Pinecone deleteAll error: {response.StatusCode} - {errorText}");
                }

                _logger.LogInformation("Successfully deleted all vectors from Pinecone index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting all vectors from Pinecone");
                throw;
            }
        }
    }
}