using BitNow_Backend.BLL.IServices;
using BitNow_Backend.DAL.DTOs;
using Microsoft.Extensions.Logging;

namespace BitNow_Backend.BLL.Services
{
    /// <summary>
    /// Recommendation service sử dụng vector similarity search với Pinecone để chọn ra các item phù hợp cho người dùng.
    /// </summary>
    public class RecommendationService : IRecommendationService
    {
        private readonly IItemService _itemService;
        private readonly IBidService _bidService;
        private readonly IWatchlistService _watchlistService;
        private readonly IEmbeddingService _embeddingService;
        private readonly IPineconeService _pineconeService;
        private readonly ILogger<RecommendationService> _logger;

        public RecommendationService(
            IItemService itemService,
            IBidService bidService,
            IWatchlistService watchlistService,
            IEmbeddingService embeddingService,
            IPineconeService pineconeService,
            ILogger<RecommendationService> logger)
        {
            _itemService = itemService;
            _bidService = bidService;
            _watchlistService = watchlistService;
            _embeddingService = embeddingService;
            _pineconeService = pineconeService;
            _logger = logger;
        }

        public async Task<IEnumerable<ItemResponseDto>> GetPersonalizedItemsAsync(int userId, int limit, CancellationToken cancellationToken = default)
        {
            if (userId <= 0)
            {
                throw new ArgumentException("userId must be greater than 0", nameof(userId));
            }

            if (limit < 1) limit = 8;
            if (limit > 24) limit = 24;

            // Lấy tất cả item có đấu giá đang active
            var allApprovedItems = await _itemService.GetAllApprovedItemsAsync();
            var candidateItems = allApprovedItems
                .Where(i =>
                    i.AuctionId.HasValue &&
                    string.Equals(i.AuctionStatus, "active", StringComparison.OrdinalIgnoreCase) &&
                    (!i.AuctionEndTime.HasValue || i.AuctionEndTime > DateTime.UtcNow))
                .ToList();

            if (!candidateItems.Any())
            {
                return candidateItems;
            }

            // Lấy lịch sử đấu giá và watchlist để tạo textbuyer
            var biddingHistory = await _bidService.GetBiddingHistoryAsync(userId, 1, 20);
            var watchlistItems = (await _watchlistService.GetByUserAsync(userId)).Take(50).ToList();

            // Kiểm tra nếu user mới (chưa có lịch sử đấu giá hoặc watchlist)
            var hasUserData = biddingHistory.Data.Any() || watchlistItems.Any();

            // Nếu user mới, trả về items ngẫu nhiên từ candidate pool
            if (!hasUserData)
            {
                _logger.LogInformation("User {UserId} is new (no history/watchlist). Returning random recommendations.", userId);
                var random = new Random(userId);
                var shuffled = candidateItems.OrderBy(_ => random.Next()).Take(limit).ToList();
                return shuffled;
            }

            // Tạo textbuyer từ watchlist và bidding history
            var textbuyer = BuildTextBuyer(biddingHistory.Data, watchlistItems);

            // Chuyển textbuyer thành embedding vector
            var queryVector = await _embeddingService.GenerateEmbeddingAsync(textbuyer, cancellationToken);

            // Tìm kiếm vector tương đồng trong Pinecone (top 4)
            var topK = Math.Min(limit, 4);
            var similarResults = await _pineconeService.QuerySimilarAsync(queryVector, topK, cancellationToken: cancellationToken);

            if (!similarResults.Any())
            {
                _logger.LogWarning("No similar vectors found for user {UserId} in Pinecone.", userId);
                throw new InvalidOperationException($"No similar vectors found for user {userId}. Please ensure that active auctions have been synced to Pinecone.");
            }

            // Lấy auction IDs từ kết quả tìm kiếm
            var auctionIds = similarResults
                .Select(r => r.id.Replace("auction_", ""))
                .Where(id => int.TryParse(id, out _))
                .Select(int.Parse)
                .ToList();

            // Lấy các items tương ứng với auction IDs
            var recommendedItems = candidateItems
                .Where(i => i.AuctionId.HasValue && auctionIds.Contains(i.AuctionId.Value))
                .ToList();

            // Nếu không đủ items, thêm các items ngẫu nhiên từ candidate pool
            if (recommendedItems.Count < limit)
            {
                var remainingIds = new HashSet<int>(recommendedItems.Select(i => i.Id));
                var additionalItems = candidateItems
                    .Where(i => !remainingIds.Contains(i.Id))
                    .OrderBy(_ => new Random(userId).Next())
                    .Take(limit - recommendedItems.Count);
                recommendedItems.AddRange(additionalItems);
            }

            // Giới hạn số lượng theo limit
            return recommendedItems.Take(limit).ToList();
        }

        /// <summary>
        /// Xây dựng textbuyer từ watchlist và bidding history để tạo embedding vector.
        /// </summary>
        private static string BuildTextBuyer(IEnumerable<BiddingHistoryDto> biddingHistory, IEnumerable<WatchlistItemDto> watchlist)
        {
            var parts = new List<string>();

            // Thêm thông tin từ bidding history
            if (biddingHistory.Any())
            {
                parts.Add("Bidding history:");
                foreach (var history in biddingHistory.Take(20))
                {
                    var historyParts = new List<string>();
                    if (!string.IsNullOrWhiteSpace(history.ItemTitle))
                    {
                        historyParts.Add(history.ItemTitle);
                    }
                    if (!string.IsNullOrWhiteSpace(history.CategoryName))
                    {
                        historyParts.Add($"Category: {history.CategoryName}");
                    }
                    if (historyParts.Any())
                    {
                        parts.Add(string.Join(", ", historyParts));
                    }
                }
            }

            // Thêm thông tin từ watchlist
            if (watchlist.Any())
            {
                parts.Add("Watchlist:");
                foreach (var item in watchlist.Take(50))
                {
                    var watchlistParts = new List<string>();
                    if (!string.IsNullOrWhiteSpace(item.ItemTitle))
                    {
                        watchlistParts.Add(item.ItemTitle);
                    }
                    if (!string.IsNullOrWhiteSpace(item.CategoryName))
                    {
                        watchlistParts.Add($"Category: {item.CategoryName}");
                    }
                    if (watchlistParts.Any())
                    {
                        parts.Add(string.Join(", ", watchlistParts));
                    }
                }
            }

            return string.Join(". ", parts);
        }
    }
}
