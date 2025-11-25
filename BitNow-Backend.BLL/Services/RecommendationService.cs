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

        // Ngưỡng điểm tương đồng tối thiểu (0.0 - 1.0)
        // Items có score < threshold sẽ bị loại bỏ
        private const float SIMILARITY_THRESHOLD = 0.5f;

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

            // Tìm kiếm nhiều hơn để có đủ items sau khi filter theo threshold
            var searchLimit = Math.Min(limit, 20);
            var similarResults = await _pineconeService.QuerySimilarAsync(queryVector, searchLimit, cancellationToken: cancellationToken);

            // ✅ LỌC THEO THRESHOLD: Chỉ lấy items có score >= SIMILARITY_THRESHOLD
            var filteredResults = similarResults
                .Where(r => r.score >= SIMILARITY_THRESHOLD)
                .ToList();

            _logger.LogInformation(
                "User {UserId}: Found {TotalResults} similar vectors, {FilteredCount} passed threshold {Threshold}",
                userId, similarResults.Count, filteredResults.Count, SIMILARITY_THRESHOLD);

            // Nếu không có kết quả phù hợp (score cao), trả về empty list
            if (!filteredResults.Any())
            {
                _logger.LogWarning(
                    "User {UserId}: No items with similarity score >= {Threshold}. Returning empty recommendations.",
                    userId, SIMILARITY_THRESHOLD);

                return Enumerable.Empty<ItemResponseDto>();
            }

            // Lấy auction IDs từ kết quả đã lọc
            var auctionIds = filteredResults
                .Select(r => r.id.Replace("auction_", ""))
                .Where(id => int.TryParse(id, out _))
                .Select(int.Parse)
                .ToList();

            // Lấy các items tương ứng với auction IDs
            var recommendedItems = candidateItems
                .Where(i => i.AuctionId.HasValue && auctionIds.Contains(i.AuctionId.Value))
                .ToList();

            // Chỉ trả về các items thực sự phù hợp, không fill bằng random
            _logger.LogInformation(
                "User {UserId}: Returning {Count} recommended items (requested: {Limit})",
                userId, recommendedItems.Count, limit);

            return recommendedItems;
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