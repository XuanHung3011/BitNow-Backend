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

        // Ngưỡng điểm tương đồng tối thiểu (giảm xuống để có nhiều kết quả hơn)
        private const float SIMILARITY_THRESHOLD = 0.3f;

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

            IEnumerable<ItemResponseDto> viewedItems = Enumerable.Empty<ItemResponseDto>();

            if (viewedAuctionIds.Any())
            {

                var uniqueAuctionIds = viewedAuctionIds.ToHashSet();
                var viewedItemsFromDb = await _auctionService.GetItemsByAuctionIdsAsync(uniqueAuctionIds); 


                var itemDict = viewedItemsFromDb.ToDictionary(i => i.AuctionId!.Value);


                viewedItems = viewedAuctionIds
                    .Where(id => itemDict.ContainsKey(id))
                    .Select(id => itemDict[id])
                    .ToList();
            }
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

            // Ưu tiên category-based nếu user có watchlist hoặc viewed items gần đây (mạnh hơn Pinecone)
            var hasRecentWatchlistOrViews = watchlistItems.Any() || viewedItems.Any();
            if (hasRecentWatchlistOrViews)
            {
                var watchlistCategories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                
                // Categories từ watchlist (ưu tiên cao nhất)
                foreach (var watch in watchlistItems)
                {
                    if (!string.IsNullOrWhiteSpace(watch.CategoryName))
                    {
                        watchlistCategories.Add(watch.CategoryName);
                    }
                }
                
                // Categories từ viewed items
                foreach (var viewed in viewedItems)
                {
                    if (!string.IsNullOrWhiteSpace(viewed.CategoryName))
                    {
                        watchlistCategories.Add(viewed.CategoryName);
                    }
                }
                
                if (watchlistCategories.Any())
                {
                    _logger.LogInformation("User {UserId} has watchlist/views. Preferred categories: {Categories}. Will prioritize these in recommendations.", 
                        userId, string.Join(", ", watchlistCategories));
                }
            }

            // Tạo textbuyer và embedding
            var textbuyer = BuildTextBuyer(biddingHistory.Data, watchlistItems, searchKeywords, viewedItems);
            _logger.LogInformation("User {UserId} textbuyer:\n{TextBuyer}", userId, textbuyer);

            // Thử tạo embedding vector - nếu fail thì fallback về random recommendations
            float[]? queryVector = null;
            try
            {
                queryVector = await _vectorSyncService.GenerateEmbeddingAsync(textbuyer, cancellationToken);
                _logger.LogInformation("User {UserId}: Successfully generated embedding vector", userId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, 
                    "User {UserId}: Failed to generate embedding (embedding service unavailable). Falling back to random recommendations.", 
                    userId);
                
                // Fallback: Trả về random active auctions
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

            // Nếu không có queryVector, fallback
            if (queryVector == null || queryVector.Length == 0)
            {
                _logger.LogWarning("User {UserId}: Query vector is null or empty. Falling back to random recommendations.", userId);
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
            
            // Thử query Pinecone - nếu fail thì fallback
            List<(string id, float score)> similarResults;
            try
            {
                similarResults = await _pineconeService.QuerySimilarAsync(
                    queryVector, searchLimit, filter, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, 
                    "User {UserId}: Failed to query Pinecone. Falling back to random recommendations.", 
                    userId);
                
                // Fallback: Trả về random active auctions
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

            // Nếu không có kết quả nào pass threshold, fallback về filter theo categories từ lịch sử
            if (!filteredResults.Any())
            {
                _logger.LogWarning("User {UserId}: No items passed threshold. Falling back to category-based recommendations.", userId);
                
                // Lấy categories từ lịch sử user
                var fallbackCategories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                
                // Categories từ bidding history
                foreach (var bid in biddingHistory.Data)
                {
                    if (!string.IsNullOrWhiteSpace(bid.CategoryName))
                    {
                        fallbackCategories.Add(bid.CategoryName);
                    }
                }
                
                // Categories từ watchlist
                foreach (var watch in watchlistItems)
                {
                    if (!string.IsNullOrWhiteSpace(watch.CategoryName))
                    {
                        fallbackCategories.Add(watch.CategoryName);
                    }
                }
                
                // Categories từ viewed items
                foreach (var viewed in viewedItems)
                {
                    if (!string.IsNullOrWhiteSpace(viewed.CategoryName))
                    {
                        fallbackCategories.Add(viewed.CategoryName);
                    }
                }
                
                // Nếu có categories ưa thích, filter theo categories
                if (fallbackCategories.Any())
                {
                    _logger.LogInformation("User {UserId}: Filtering by preferred categories: {Categories}", 
                        userId, string.Join(", ", fallbackCategories));
                    
                    var approvedItemsForFallback = await _itemService.GetAllApprovedItemsAsync();
                    var categoryBasedItems = approvedItemsForFallback
                        .Where(i =>
                            i.AuctionId.HasValue &&
                            string.Equals(i.AuctionStatus, "active", StringComparison.OrdinalIgnoreCase) &&
                            (!i.AuctionEndTime.HasValue || i.AuctionEndTime > DateTime.UtcNow) &&
                            !string.IsNullOrWhiteSpace(i.CategoryName) &&
                            fallbackCategories.Contains(i.CategoryName))
                        .ToList();
                    
                    if (categoryBasedItems.Any())
                    {
                        var random = new Random(userId);
                        return categoryBasedItems.OrderBy(_ => random.Next()).Take(limit).ToList();
                    }
                }
                
                // Nếu vẫn không có, fallback về random
                _logger.LogWarning("User {UserId}: No category-based items found. Falling back to random recommendations.", userId);
                var approvedItemsRandom = await _itemService.GetAllApprovedItemsAsync();
                var activeItemsRandom = approvedItemsRandom
                    .Where(i =>
                        i.AuctionId.HasValue &&
                        string.Equals(i.AuctionStatus, "active", StringComparison.OrdinalIgnoreCase) &&
                        (!i.AuctionEndTime.HasValue || i.AuctionEndTime > DateTime.UtcNow))
                    .ToList();

                var randomFinal = new Random(userId);
                return activeItemsRandom.OrderBy(_ => randomFinal.Next()).Take(limit).ToList();
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

            // Lấy categories ưa thích từ lịch sử để boost items cùng category
            var preferredCategories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            
            // Categories từ watchlist (ưu tiên cao nhất)
            foreach (var watch in watchlistItems)
            {
                if (!string.IsNullOrWhiteSpace(watch.CategoryName))
                {
                    preferredCategories.Add(watch.CategoryName);
                }
            }
            
            // Categories từ viewed items (ưu tiên cao)
            foreach (var viewed in viewedItems)
            {
                if (!string.IsNullOrWhiteSpace(viewed.CategoryName))
                {
                    preferredCategories.Add(viewed.CategoryName);
                }
            }
            
            // Categories từ bidding history
            foreach (var bid in biddingHistory.Data)
            {
                if (!string.IsNullOrWhiteSpace(bid.CategoryName))
                {
                    preferredCategories.Add(bid.CategoryName);
                }
            }

            // Boost score cho items cùng category với preferences
            var recommendedItems = items
                .Where(i => i.AuctionId.HasValue)
                .Select(i =>
                {
                    var baseScore = scoreDict.GetValueOrDefault(i.AuctionId!.Value, 0f);
                    // Boost score nếu cùng category với preferences
                    if (!string.IsNullOrWhiteSpace(i.CategoryName) && preferredCategories.Contains(i.CategoryName))
                    {
                        baseScore += 0.2f; // Boost 0.2 điểm
                    }
                    return new { Item = i, Score = baseScore };
                })
                .OrderByDescending(x => x.Score)
                .Take(limit)
                .Select(x => x.Item)
                .ToList();

            // Nếu không đủ items từ Pinecone, bổ sung bằng category-based
            if (recommendedItems.Count < limit && preferredCategories.Any())
            {
                _logger.LogInformation("User {UserId}: Only {Count} items from Pinecone, supplementing with category-based items", 
                    userId, recommendedItems.Count);
                
                var existingIds = recommendedItems.Where(i => i.AuctionId.HasValue).Select(i => i.AuctionId!.Value).ToHashSet();
                
                var approvedItems = await _itemService.GetAllApprovedItemsAsync();
                var categoryBasedItems = approvedItems
                    .Where(i =>
                        i.AuctionId.HasValue &&
                        !existingIds.Contains(i.AuctionId.Value) &&
                        string.Equals(i.AuctionStatus, "active", StringComparison.OrdinalIgnoreCase) &&
                        (!i.AuctionEndTime.HasValue || i.AuctionEndTime > DateTime.UtcNow) &&
                        !string.IsNullOrWhiteSpace(i.CategoryName) &&
                        preferredCategories.Contains(i.CategoryName))
                    .Take(limit - recommendedItems.Count)
                    .ToList();
                
                recommendedItems.AddRange(categoryBasedItems);
            }

            _logger.LogInformation(
                "User {UserId}: Returning {Count} items. Preferred categories: {Categories}",
                userId,
                recommendedItems.Count,
                preferredCategories.Any() ? string.Join(", ", preferredCategories) : "none");

            _logger.LogInformation(
                "User {UserId}: Items {@Items}",
                userId,
                recommendedItems.Select(i => new {
                    AuctionId = i.AuctionId,
                    Title = i.Title,
                    Category = i.CategoryName,
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
            var categorySet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Thêm thông tin từ bidding history - nhấn mạnh categories
            if (biddingHistory.Any())
            {
                var biddingItems = biddingHistory
                    .Take(20)
                    .Select(h =>
                    {
                        var itemParts = new List<string>();
                        if (!string.IsNullOrWhiteSpace(h.CategoryName))
                        {
                            categorySet.Add(h.CategoryName);
                            itemParts.Add($"Category: {h.CategoryName}");
                        }
                        if (!string.IsNullOrWhiteSpace(h.ItemTitle))
                            itemParts.Add(h.ItemTitle);
                        return itemParts.Any() ? string.Join(". ", itemParts) : null;
                    })
                    .Where(s => !string.IsNullOrWhiteSpace(s));

                if (biddingItems.Any())
                {
                    parts.Add($"Bidding history: {string.Join(", ", biddingItems)}");
                }
            }

            // Thêm thông tin từ watchlist - nhấn mạnh categories
            if (watchlist.Any())
            {
                var watchlistItems = watchlist
                    .Take(20)
                    .Select(w =>
                    {
                        var itemParts = new List<string>();
                        if (!string.IsNullOrWhiteSpace(w.CategoryName))
                        {
                            categorySet.Add(w.CategoryName);
                            itemParts.Add($"Category: {w.CategoryName}");
                        }
                        if (!string.IsNullOrWhiteSpace(w.ItemTitle))
                            itemParts.Add(w.ItemTitle);
                        return itemParts.Any() ? string.Join(". ", itemParts) : null;
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

            // Thêm thông tin từ các auction mà user hay xem - nhấn mạnh categories
            if (viewedItems != null && viewedItems.Any())
            {
                var viewed = viewedItems
                    .Take(20)
                    .Select(v =>
                    {
                        var itemParts = new List<string>();
                        if (!string.IsNullOrWhiteSpace(v.CategoryName))
                        {
                            categorySet.Add(v.CategoryName);
                            itemParts.Add($"Category: {v.CategoryName}");
                        }
                        if (!string.IsNullOrWhiteSpace(v.Title))
                            itemParts.Add(v.Title);
                        return itemParts.Any() ? string.Join(". ", itemParts) : null;
                    })
                    .Where(s => !string.IsNullOrWhiteSpace(s));

                if (viewed.Any())
                {
                    parts.Add($"Frequently viewed auctions: {string.Join(", ", viewed)}");
                }
            }

            // Thêm phần tổng hợp categories ở cuối để nhấn mạnh
            if (categorySet.Any())
            {
                parts.Add($"Preferred categories: {string.Join(", ", categorySet)}");
            }

            return string.Join("\n", parts);
        }
    }
}
