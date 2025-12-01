using BitNow_Backend.BLL.IServices;
using BitNow_Backend.DAL.DTOs;
using Microsoft.Extensions.Logging;

namespace BitNow_Backend.BLL.Services
{

    /// Recommendation service sử dụng vector similarity search với Pinecone để chọn ra các item phù hợp cho người dùng.
    public class RecommendationService : IRecommendationService
    {
        private readonly IItemService _itemService;
        private readonly IBidService _bidService;
        private readonly IWatchlistService _watchlistService;
        private readonly ISearchKeywordService _searchKeywordService;
        private readonly IVectorSyncService _vectorSyncService;
        private readonly IPineconeService _pineconeService;
        private readonly IAuctionService _auctionService;
        private readonly ILogger<RecommendationService> _logger;

        // Ngưỡng điểm tương đồng tối thiểu 
        private const float SIMILARITY_THRESHOLD = 0.5f;

        public RecommendationService(
            IItemService itemService,
            IBidService bidService,
            IWatchlistService watchlistService,
            ISearchKeywordService searchKeywordService,
            IVectorSyncService vectorSyncService,
            IPineconeService pineconeService,
            IAuctionService auctionService,
            ILogger<RecommendationService> logger)
        {
            _itemService = itemService;
            _bidService = bidService;
            _watchlistService = watchlistService;
            _searchKeywordService = searchKeywordService;
            _vectorSyncService = vectorSyncService;
            _pineconeService = pineconeService;
            _auctionService = auctionService;
            _logger = logger;
        }

        public async Task<IEnumerable<ItemResponseDto>> GetPersonalizedItemsAsync(
            int userId, int limit, CancellationToken cancellationToken = default)
        {
            if (userId <= 0)
            {
                throw new ArgumentException("userId must be greater than 0", nameof(userId));
            }

            if (limit < 1) limit = 4;
            if (limit > 24) limit = 24;

            // Lấy lịch sử user
            var biddingHistory = await _bidService.GetBiddingHistoryAsync(userId, 1, 20);
            var watchlistItems = (await _watchlistService.GetByUserAsync(userId)).Take(50).ToList();
            var searchKeywords = await _searchKeywordService.GetRecentKeywordsAsync(userId, 20);
            var hasUserData = biddingHistory.Data.Any() || watchlistItems.Any() || searchKeywords.Any();

            // Fallback cho new users
            if (!hasUserData)
            {
                _logger.LogInformation("User {UserId} is new. Returning random recommendations.", userId);
                var approvedItemsForFallback = await _itemService.GetAllApprovedItemsAsync();
                var activeItems = approvedItemsForFallback
                    .Where(i =>
                        i.AuctionId.HasValue &&
                        string.Equals(i.AuctionStatus, "active", StringComparison.OrdinalIgnoreCase) &&
                        (!i.AuctionEndTime.HasValue || i.AuctionEndTime > DateTime.UtcNow))
                    .ToList();

                var random = new Random(userId);
                return activeItems.OrderBy(_ => random.Next()).Take(limit).ToList();
            }

            // Tạo textbuyer và embedding
            var textbuyer = BuildTextBuyer(biddingHistory.Data, watchlistItems, searchKeywords);
            _logger.LogInformation("User {UserId} textbuyer:\n{TextBuyer}", userId, textbuyer);

            //  Gọi GenerateEmbeddingAsync từ VectorSyncService
            var queryVector = await _vectorSyncService.GenerateEmbeddingAsync(textbuyer, cancellationToken);

            // Query Pinecone với filter
            var currentTimeUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var filter = new Dictionary<string, object>
            {
                ["$and"] = new[]
                {
                    new Dictionary<string, object>
                    {
                        ["status"] = new Dictionary<string, object> { ["$in"] = new[] { "active", "scheduled" } }
                    },
                    new Dictionary<string, object>
                    {
                        ["$or"] = new[]
                        {
                            new Dictionary<string, object>
                            {
                                ["endTimeUnix"] = new Dictionary<string, object> { ["$eq"] = 0 }
                            },
                            new Dictionary<string, object>
                            {
                                ["endTimeUnix"] = new Dictionary<string, object> { ["$gt"] = currentTimeUnix }
                            }
                        }
                    }
                }
            };

            var searchLimit = Math.Max(limit * 2, 20);
            var similarResults = await _pineconeService.QuerySimilarAsync(
                queryVector, searchLimit, filter, cancellationToken);

            // Log top results
            _logger.LogInformation(
                "User {UserId}: Pinecone returned {@TopResults}",
                userId,
                similarResults.Take(10).Select(r => new { r.id, score = r.score.ToString("F3") }));

            // Filter theo threshold
            var filteredResults = similarResults
                .Where(r => r.score >= SIMILARITY_THRESHOLD)
                .ToList();

            _logger.LogInformation(
                "User {UserId}: {TotalResults} total, {FilteredCount} passed threshold {Threshold}",
                userId, similarResults.Count, filteredResults.Count, SIMILARITY_THRESHOLD);

            if (!filteredResults.Any())
            {
                _logger.LogWarning("User {UserId}: No items passed threshold", userId);
                return Enumerable.Empty<ItemResponseDto>();
            }

            // Lấy auction IDs
            var auctionIds = filteredResults
                .Select(r => r.id.Replace("auction_", ""))
                .Where(id => int.TryParse(id, out _))
                .Select(int.Parse)
                .ToHashSet();

            // Tạo score dictionary
            var scoreDict = filteredResults.ToDictionary(
                r => int.Parse(r.id.Replace("auction_", "")),
                r => r.score
            );

            // Lấy items và sort theo similarity score
            var items = await _auctionService.GetItemsByAuctionIdsAsync(auctionIds);
            

            var recommendedItems = items
            .Where(i => i.AuctionId.HasValue)
            .OrderByDescending(i => scoreDict.GetValueOrDefault(i.AuctionId!.Value, 0f))
            .Take(limit)
            .ToList();

            _logger.LogInformation(
                "User {UserId}: Returning {@Items}",
                userId,
                recommendedItems.Select(i => new {
                    AuctionId = i.AuctionId,
                    Title = i.Title,
                    Category = i.CategoryName,
                    Score = scoreDict.GetValueOrDefault(i.AuctionId!.Value, 0f).ToString("F3")
                }));

            return recommendedItems;
        }


        /// Xây dựng textbuyer từ watchlist, bidding history và search keywords để tạo embedding vector.
        private static string BuildTextBuyer(
            IEnumerable<BiddingHistoryDto> biddingHistory,
            IEnumerable<WatchlistItemDto> watchlist,
            IEnumerable<string> searchKeywords)
        {
            var parts = new List<string>();

            // Thêm thông tin từ bidding history (chỉ lấy Title, bỏ Category)
            if (biddingHistory.Any())
            {
                var biddingItems = biddingHistory
                    .Take(20)
                    .Where(h => !string.IsNullOrWhiteSpace(h.ItemTitle))
                    .Select(h => h.ItemTitle);

                if (biddingItems.Any())
                {
                    parts.Add($"Bidding history: {string.Join(", ", biddingItems)}");
                }
            }

            // Thêm thông tin từ watchlist (chỉ lấy Title, bỏ Category)
            if (watchlist.Any())
            {
                var watchlistItems = watchlist
                    .Take(50)
                    .Where(w => !string.IsNullOrWhiteSpace(w.ItemTitle))
                    .Select(w => w.ItemTitle);

                if (watchlistItems.Any())
                {
                    parts.Add($"Watchlist: {string.Join(", ", watchlistItems)}");
                }
            }

            // Thêm thông tin từ các từ khóa tìm kiếm gần đây
            if (searchKeywords != null && searchKeywords.Any())
            {
                parts.Add($"Recent search keywords: {string.Join(", ", searchKeywords)}");
            }

            return string.Join("\n", parts);
        }
    }
}