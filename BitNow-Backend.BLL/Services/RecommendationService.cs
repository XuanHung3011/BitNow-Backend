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
        private readonly IUserAuctionViewService _userAuctionViewService;
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
            IUserAuctionViewService userAuctionViewService,
            IVectorSyncService vectorSyncService,
            IPineconeService pineconeService,
            IAuctionService auctionService,
            ILogger<RecommendationService> logger)
        {
            _itemService = itemService;
            _bidService = bidService;
            _watchlistService = watchlistService;
            _searchKeywordService = searchKeywordService;
            _userAuctionViewService = userAuctionViewService;
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
            var watchlistItems = (await _watchlistService.GetByUserAsync(userId)).Take(20).ToList();
            var searchKeywords = await _searchKeywordService.GetRecentKeywordsAsync(userId, 20);
            var viewedAuctionIds = await _userAuctionViewService.GetRecentViewedAuctionIdsAsync(userId, 20, cancellationToken);
            var viewedItems = viewedAuctionIds.Any()
                ? await _auctionService.GetItemsByAuctionIdsAsync(viewedAuctionIds.ToHashSet())
                : Enumerable.Empty<ItemResponseDto>();

            var hasUserData = biddingHistory.Data.Any() || watchlistItems.Any() || searchKeywords.Any() || viewedAuctionIds.Any();

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
            var textbuyer = BuildTextBuyer(biddingHistory.Data, watchlistItems, searchKeywords, viewedItems);
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
                                ["endTime"] = new Dictionary<string, object> { ["$eq"] = 0 }
                            },
                            new Dictionary<string, object>
                            {
                                ["endTime"] = new Dictionary<string, object> { ["$gt"] = currentTimeUnix }
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
                    Score = scoreDict.GetValueOrDefault(i.AuctionId!.Value, 0f).ToString("F3")
                }));

            return recommendedItems;
        }


        private static string BuildTextBuyer(
            IEnumerable<BiddingHistoryDto> biddingHistory,
            IEnumerable<WatchlistItemDto> watchlist,
            IEnumerable<string> searchKeywords,
            IEnumerable<ItemResponseDto> viewedItems)
        {
            var parts = new List<string>();

            // Thêm thông tin từ bidding history 
            if (biddingHistory.Any())
            {
                var biddingItems = biddingHistory
                    .Take(20)
                    .Select(h =>
                    {
                        var itemParts = new List<string>();
                        if (!string.IsNullOrWhiteSpace(h.ItemTitle))
                            itemParts.Add(h.ItemTitle);
                        if (!string.IsNullOrWhiteSpace(h.CategoryName))
                            itemParts.Add($"({h.CategoryName})");
                        return itemParts.Any() ? string.Join(" ", itemParts) : null;
                    })
                    .Where(s => !string.IsNullOrWhiteSpace(s));

                if (biddingItems.Any())
                {
                    parts.Add($"Bidding history: {string.Join(", ", biddingItems)}");
                }
            }

            // Thêm thông tin từ watchlist 
            if (watchlist.Any())
            {
                var watchlistItems = watchlist
                    .Take(20)
                    .Select(w =>
                    {
                        var itemParts = new List<string>();
                        if (!string.IsNullOrWhiteSpace(w.ItemTitle))
                            itemParts.Add(w.ItemTitle);
                        if (!string.IsNullOrWhiteSpace(w.CategoryName))
                            itemParts.Add($"({w.CategoryName})");
                        return itemParts.Any() ? string.Join(" ", itemParts) : null;
                    })
                    .Where(s => !string.IsNullOrWhiteSpace(s));

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

            // Thêm thông tin từ các auction mà user hay xem
            if (viewedItems != null && viewedItems.Any())
            {
                var viewed = viewedItems
                    .Take(20)
                    .Select(v =>
                    {
                        var itemParts = new List<string>();
                        if (!string.IsNullOrWhiteSpace(v.Title))
                            itemParts.Add(v.Title);
                        if (!string.IsNullOrWhiteSpace(v.CategoryName))
                            itemParts.Add($"({v.CategoryName})");
                        return itemParts.Any() ? string.Join(" ", itemParts) : null;
                    })
                    .Where(s => !string.IsNullOrWhiteSpace(s));

                if (viewed.Any())
                {
                    parts.Add($"Frequently viewed auctions: {string.Join(", ", viewed)}");
                }
            }

            return string.Join("\n", parts);
        }
    }
}
