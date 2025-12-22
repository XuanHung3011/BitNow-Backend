using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using BitNow_Backend.BLL.IServices;
using BitNow_Backend.DAL.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BitNow_Backend.BLL.Services
{

    public class VectorSyncService : IVectorSyncService
    {
        private readonly IItemService _itemService;
        private readonly IPineconeService _pineconeService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<VectorSyncService> _logger;

        public VectorSyncService(
            IItemService itemService,
            IPineconeService pineconeService,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<VectorSyncService> logger)
        {
            _itemService = itemService;
            _pineconeService = pineconeService;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

       


        /// Tạo embedding vector từ text sử dụng LM Studio (local) với Nomic Embed model.
        public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new ArgumentException("Text cannot be null or empty", nameof(text));
            }

            try
            {
                var baseUrl = _configuration["LMStudio:BaseUrl"] ?? "http://localhost:1234";
                var model = _configuration["LMStudio:Model"] ?? "nomic-embed-text-v2-moe";
                
                // Detect Ollama vs LMStudio based on port or URL
                var isOllama = baseUrl.Contains("11434") || baseUrl.Contains("ollama");
                var endpoint = isOllama ? "/api/embeddings" : "/v1/embeddings";
                var inputField = isOllama ? "prompt" : "input";

                var client = _httpClientFactory.CreateClient("LMStudio");
                client.BaseAddress = new Uri(baseUrl);
                client.Timeout = TimeSpan.FromSeconds(30);

                // Ollama uses "prompt", LMStudio uses "input"
                var payload = new Dictionary<string, object>
                {
                    ["model"] = model
                };
                payload[inputField] = text;

                var json = JsonSerializer.Serialize(payload);
                using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };

                using var response = await client.SendAsync(request, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorText = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError("Embedding API ({Service}) returned non-success status {Status}: {Body}",
                        isOllama ? "Ollama" : "LMStudio", response.StatusCode, errorText);
                    throw new InvalidOperationException($"Embedding API error: {response.StatusCode} - {errorText}");
                }

                using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var document = await JsonDocument.ParseAsync(contentStream, cancellationToken: cancellationToken);

                var root = document.RootElement;
                
                // Ollama returns embedding directly, LMStudio wraps in "data" array
                JsonElement embeddingElement;
                if (root.TryGetProperty("data", out var data))
                {
                    // LMStudio format
                    if (data.GetArrayLength() == 0)
                    {
                        throw new InvalidOperationException("Embedding API returned empty embedding data");
                    }
                    embeddingElement = data[0].GetProperty("embedding");
                }
                else if (root.TryGetProperty("embedding", out embeddingElement))
                {
                    // Ollama format - direct embedding
                }
                else
                {
                    throw new InvalidOperationException("Invalid embedding API response format");
                }

                var embeddingArray = new List<float>();
                foreach (var element in embeddingElement.EnumerateArray())
                {
                    embeddingArray.Add((float)element.GetDouble());
                }

                _logger.LogInformation("Generated embedding with {Dimensions} dimensions using {Service}",
                    embeddingArray.Count, isOllama ? "Ollama" : "LMStudio");

                return embeddingArray.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating embedding for text: {Text}", text);
                throw;
            }
        }



        public async Task SyncActiveAuctionsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Starting sync of active auctions to Pinecone");

                var allItems = await _itemService.GetAllApprovedItemsAsync();

                var activeItems = allItems
                    .Where(i =>
                        i.AuctionId.HasValue &&
                        new[] { "active", "scheduled" }
                            .Contains(i.AuctionStatus?.ToLower()) &&
                        (!i.AuctionEndTime.HasValue || i.AuctionEndTime > DateTime.UtcNow))
                    .ToList();

                _logger.LogInformation("Found {Count} active/scheduled auctions to sync", activeItems.Count);

                var successCount = 0;
                var errorCount = 0;

                foreach (var item in activeItems)
                {
                    try
                    {
                        await SyncAuctionAsync(item, cancellationToken);
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        errorCount++;
                        _logger.LogWarning(ex, "Failed to sync auction {AuctionId} (Item {ItemId})",
                            item.AuctionId, item.Id);
                    }
                }

                _logger.LogInformation("Sync completed: {SuccessCount} succeeded, {ErrorCount} failed",
                    successCount, errorCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing active auctions to Pinecone");
                throw;
            }
        }


        public async Task SyncAuctionAsync(ItemResponseDto item, CancellationToken cancellationToken = default)
        {
            if (item.AuctionId == null)
            {
                throw new ArgumentException("Item must have an AuctionId", nameof(item));
            }

            try
            {
                // Tạo text representation
                var textRepresentation = BuildItemText(item);
                _logger.LogInformation("Text for embedding: {Text}", textRepresentation);

                // Tạo embedding vector
                var embedding = await GenerateEmbeddingAsync(textRepresentation, cancellationToken);
                _logger.LogInformation("Generated embedding with {Dimensions} dimensions", embedding.Length);

                // Chuẩn bị metadata 
                var metadata = new Dictionary<string, object>
                {
                    { "itemId", item.Id },
                    { "auctionId", item.AuctionId.Value },
                    { "title", item.Title ?? "" },
                    { "description", item.Description ?? "" },
                    { "status", item.AuctionStatus ?? "active" },
                    { "endTime", item.AuctionEndTime.HasValue
                        ? new DateTimeOffset(item.AuctionEndTime.Value).ToUnixTimeSeconds()
                        : 0 }
                };

                var vectorId = $"auction_{item.AuctionId.Value}";
                _logger.LogInformation("Upserting vector with ID: {VectorId}", vectorId);

                // Upsert vào Pinecone
                await _pineconeService.UpsertVectorAsync(
                    vectorId,
                    embedding,
                    metadata,
                    cancellationToken);

                _logger.LogInformation(" Successfully synced auction {AuctionId} to Pinecone",
                    item.AuctionId.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, " Error syncing auction {AuctionId}: {Message}",
                    item.AuctionId, ex.Message);
                throw;
            }
        }

        public async Task RemoveExpiredAuctionsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Starting removal of expired auctions from Pinecone");

                var allItems = await _itemService.GetAllApprovedItemsAsync();
                var expiredItems = allItems
                    .Where(i =>
                        i.AuctionId.HasValue &&
                        (i.AuctionEndTime.HasValue && i.AuctionEndTime <= DateTime.UtcNow ||
                         string.Equals(i.AuctionStatus, "completed", StringComparison.OrdinalIgnoreCase) || 
                         string.Equals(i.AuctionStatus, "paused", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(i.AuctionStatus, "cancelled", StringComparison.OrdinalIgnoreCase)))
                    .ToList();

                if (!expiredItems.Any())
                {
                    _logger.LogInformation("No expired auctions to remove");
                    return;
                }

                var idsToDelete = expiredItems
                    .Where(i => i.AuctionId.HasValue)
                    .Select(i => $"auction_{i.AuctionId.Value}")
                    .ToList();

                _logger.LogInformation("Removing expired auctions from Pinecone");

                await _pineconeService.DeleteVectorsAsync(idsToDelete, cancellationToken);

                _logger.LogInformation("Successfully removed  expired auctions from Pinecone");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing expired auctions from Pinecone");
                throw;
            }
        }

        public async Task ClearAllVectorsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogWarning("Starting deletion of ALL vectors from Pinecone");

                await _pineconeService.DeleteAllVectorsAsync(cancellationToken);

                _logger.LogWarning("Successfully deleted all vectors from Pinecone");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing all vectors from Pinecone");
                throw;
            }
        }


        /// Tạo text representation của item để embedding.
        private static string BuildItemText(ItemResponseDto item)
        {
            var parts = new List<string>();

            if (!string.IsNullOrWhiteSpace(item.Title))
            {
                parts.Add(item.Title);
            }

            if (!string.IsNullOrWhiteSpace(item.Description))
            {
                parts.Add(item.Description);
            }
            if (!string.IsNullOrWhiteSpace(item.CategoryName))
            {
                parts.Add($"Category: {item.CategoryName}");
            }

            if (!string.IsNullOrWhiteSpace(item.Condition))
            {
                parts.Add($"Condition: {item.Condition}");
            }

            return string.Join(". ", parts);
        }

     
    }
}