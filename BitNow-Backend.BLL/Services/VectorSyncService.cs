using BitNow_Backend.BLL.IServices;
using BitNow_Backend.DAL.DTOs;
using Microsoft.Extensions.Logging;

namespace BitNow_Backend.BLL.Services
{
    /// <summary>
    /// Service đồng bộ dữ liệu phiên đấu giá vào Pinecone vector database.
    /// </summary>
    public class VectorSyncService : IVectorSyncService
    {
        private readonly IItemService _itemService;
        private readonly IEmbeddingService _embeddingService;
        private readonly IPineconeService _pineconeService;
        private readonly ILogger<VectorSyncService> _logger;

        public VectorSyncService(
            IItemService itemService,
            IEmbeddingService embeddingService,
            IPineconeService pineconeService,
            ILogger<VectorSyncService> logger)
        {
            _itemService = itemService;
            _embeddingService = embeddingService;
            _pineconeService = pineconeService;
            _logger = logger;
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
                        string.Equals(i.AuctionStatus, "active", StringComparison.OrdinalIgnoreCase) &&
                        (!i.AuctionEndTime.HasValue || i.AuctionEndTime > DateTime.UtcNow))
                    .ToList();

                _logger.LogInformation("Found {Count} active auctions to sync", activeItems.Count);

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
                        _logger.LogWarning(ex, "Failed to sync auction {AuctionId} (Item {ItemId})", item.AuctionId, item.Id);
                    }
                }

                _logger.LogInformation("Sync completed: {SuccessCount} succeeded, {ErrorCount} failed", successCount, errorCount);
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
                var textRepresentation = BuildItemText(item);
                _logger.LogInformation("Text for embedding: {Text}", textRepresentation);

                var embedding = await _embeddingService.GenerateEmbeddingAsync(textRepresentation, cancellationToken);
                _logger.LogInformation("Generated embedding with {Dimensions} dimensions", embedding.Length);

                // ✅ Thêm endTimeUnix và status vào metadata để filter trên Pinecone
                var metadata = new Dictionary<string, object>
                {
                    { "itemId", item.Id },
                    { "auctionId", item.AuctionId.Value },
                    { "title", item.Title ?? "" },
                    { "category", item.CategoryName ?? "" },
                    { "description", item.Description ?? "" },
                    { "basePrice", item.BasePrice?.ToString() ?? "" },
                    { "currentBid", item.CurrentBid?.ToString() ?? "" },
                    { "status", item.AuctionStatus ?? "active" },
                    // ✅ Lưu end time dưới dạng Unix timestamp (seconds) để Pinecone filter được
                    { "endTimeUnix", item.AuctionEndTime.HasValue
                        ? new DateTimeOffset(item.AuctionEndTime.Value).ToUnixTimeSeconds()
                        : 0 }
                };

                var vectorId = $"auction_{item.AuctionId.Value}";
                _logger.LogInformation("Upserting vector with ID: {VectorId}", vectorId);

                await _pineconeService.UpsertVectorAsync(
                    vectorId,
                    embedding,
                    metadata,
                    cancellationToken);

                _logger.LogInformation("✅ Successfully synced auction {AuctionId} to Pinecone", item.AuctionId.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error syncing auction {AuctionId}: {Message}", item.AuctionId, ex.Message);
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

                _logger.LogInformation("Removing {Count} expired auctions from Pinecone", idsToDelete.Count);

                await _pineconeService.DeleteVectorsAsync(idsToDelete, cancellationToken);

                _logger.LogInformation("Successfully removed {Count} expired auctions from Pinecone", idsToDelete.Count);
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

            if (!string.IsNullOrWhiteSpace(item.Location))
            {
                parts.Add($"Location: {item.Location}");
            }

            if (item.BasePrice.HasValue)
            {
                parts.Add($"Base price: {item.BasePrice.Value}");
            }

            if (item.CurrentBid.HasValue)
            {
                parts.Add($"Current bid: {item.CurrentBid.Value}");
            }

            return string.Join(". ", parts);
        }
    }
}