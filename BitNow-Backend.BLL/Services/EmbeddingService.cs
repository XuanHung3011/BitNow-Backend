using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using BitNow_Backend.BLL.IServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BitNow_Backend.BLL.Services
{
    /// <summary>
    /// Service tạo embedding vector sử dụng LM Studio (local) với Nomic Embed model.
    /// </summary>
    public class EmbeddingService : IEmbeddingService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmbeddingService> _logger;

        public EmbeddingService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<EmbeddingService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new ArgumentException("Text cannot be null or empty", nameof(text));
            }

            try
            {
                var lmStudioUrl = _configuration["LMStudio:BaseUrl"] ?? "http://localhost:1234";
                var model = _configuration["LMStudio:Model"] ?? "nomic-embed-text-v1.5";

                var client = _httpClientFactory.CreateClient("LMStudio");
                client.BaseAddress = new Uri(lmStudioUrl);
                client.Timeout = TimeSpan.FromSeconds(30);

                var payload = new
                {
                    model = model,
                    input = text
                };

                var json = JsonSerializer.Serialize(payload);
                using var request = new HttpRequestMessage(HttpMethod.Post, "/v1/embeddings")
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };

                using var response = await client.SendAsync(request, cancellationToken);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorText = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError("LM Studio API returned non-success status {Status}: {Body}", response.StatusCode, errorText);
                    throw new InvalidOperationException($"LM Studio API error: {response.StatusCode} - {errorText}");
                }

                using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var document = await JsonDocument.ParseAsync(contentStream, cancellationToken: cancellationToken);

                var root = document.RootElement;
                var data = root.GetProperty("data");
                if (data.GetArrayLength() == 0)
                {
                    throw new InvalidOperationException("LM Studio returned empty embedding data");
                }

                var embedding = data[0].GetProperty("embedding");
                var embeddingArray = new List<float>();
                
                foreach (var element in embedding.EnumerateArray())
                {
                    embeddingArray.Add((float)element.GetDouble());
                }

                return embeddingArray.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating embedding for text: {Text}", text);
                throw;
            }
        }
    }
}

