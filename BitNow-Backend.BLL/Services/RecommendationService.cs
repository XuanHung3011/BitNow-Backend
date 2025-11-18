using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using BitNow_Backend.BLL.IServices;
using BitNow_Backend.DAL.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BitNow_Backend.BLL.Services
{
    /// <summary>
    /// Recommendation service sử dụng OpenAI (ChatGPT API) để chọn ra các item phù hợp cho người dùng.
    /// </summary>
    public class RecommendationService : IRecommendationService
    {
        private readonly IItemService _itemService;
        private readonly IBidService _bidService;
        private readonly IAuctionService _auctionService;
        private readonly IWatchlistService _watchlistService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<RecommendationService> _logger;

        public RecommendationService(
            IItemService itemService,
            IBidService bidService,
            IAuctionService auctionService,
            IWatchlistService watchlistService,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<RecommendationService> logger)
        {
            _itemService = itemService;
            _bidService = bidService;
            _auctionService = auctionService;
            _watchlistService = watchlistService;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        private sealed class OpenAiRecommendationResponse
        {
            public List<int> ItemIds { get; set; } = new();
        }

        public async Task<IEnumerable<ItemResponseDto>> GetPersonalizedItemsAsync(int userId, int limit, CancellationToken cancellationToken = default)
        {
            if (userId <= 0)
            {
                throw new ArgumentException("userId must be greater than 0", nameof(userId));
            }

            if (limit < 1) limit = 8;
            if (limit > 24) limit = 24;

            // Lấy tất cả item có đấu giá đang active để AI chọn (ưu tiên đa dạng, không chỉ hot)
            var allApprovedItems = await _itemService.GetAllApprovedItemsAsync();
            var candidateItems = allApprovedItems
                .Where(i =>
                    i.AuctionId.HasValue &&
                    string.Equals(i.AuctionStatus, "active", StringComparison.OrdinalIgnoreCase) &&
                    (!i.AuctionEndTime.HasValue || i.AuctionEndTime > DateTime.UtcNow))
                .OrderBy(i => i.AuctionEndTime ?? DateTime.MaxValue)
                .Take(Math.Clamp(limit * 6, limit, 60))
                .ToList();

            if (!candidateItems.Any())
            {
                return candidateItems;
            }

            // Lấy thêm ngữ cảnh: đấu giá đang tham gia, watchlist, lịch sử
            var biddingHistory = await _bidService.GetBiddingHistoryAsync(userId, 1, 20);
            var activeBids = await _auctionService.GetActiveBidsByBuyerAsync(userId, 1, 50);
            var watchlistItems = (await _watchlistService.GetByUserAsync(userId)).Take(50).ToList();

            // Thử gọi OpenAI, nếu lỗi thì fallback: trả về candidateItems như cũ
            try
            {
                var apiKey = _configuration["OpenAI:ApiKey"];
                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    _logger.LogWarning("OpenAI API key is not configured. Falling back to non-AI recommendations.");
                    return candidateItems.Take(limit).ToList();
                }

                var model = _configuration["OpenAI:Model"] ?? "gpt-4o-mini";

                var client = _httpClientFactory.CreateClient("OpenAI");
                client.BaseAddress = new Uri("https://api.openai.com");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

                // Chuẩn bị dữ liệu rút gọn để gửi lên AI (tránh payload quá lớn)
                var historySummary = biddingHistory.Data
                    .OrderByDescending(h => h.BidTime)
                    .Take(20)
                    .Select(h => new
                    {
                        auctionId = h.AuctionId,
                        title = h.ItemTitle,
                        category = h.CategoryName,
                        yourBid = h.YourBid,
                        status = h.Status
                    });

                var activeBidsSummary = activeBids.Data
                    .Take(30)
                    .Select(b => new
                    {
                        auctionId = b.AuctionId,
                        title = b.ItemTitle,
                        category = b.CategoryName,
                        currentBid = b.CurrentBid,
                        yourHighestBid = b.YourHighestBid,
                        isLeading = b.IsLeading,
                        endTime = b.EndTime
                    });

                var watchlistSummary = watchlistItems.Select(w => new
                {
                    auctionId = w.AuctionId,
                    title = w.ItemTitle,
                    currentBid = w.CurrentBid ?? w.StartingBid,
                    endTime = w.EndTime,
                    status = w.Status
                });

                var itemsSummary = candidateItems.Select(i => new
                {
                    itemId = i.Id,
                    auctionId = i.AuctionId,
                    title = i.Title,
                    category = i.CategoryName,
                    basePrice = i.BasePrice,
                    currentBid = i.CurrentBid,
                    bidCount = i.BidCount,
                    description = i.Description
                });

                var userPromptObject = new
                {
                    userId,
                    activeBids = activeBidsSummary,
                    watchlist = watchlistSummary,
                    history = historySummary,
                    candidates = itemsSummary,
                    limit
                };

                var userPromptJson = JsonSerializer.Serialize(userPromptObject);

                var payload = new
                {
                    model,
                    messages = new[]
                    {
                        new
                        {
                            role = "system",
                            content = "You are a recommendation engine for an online auction platform. " +
                                      "Based on the user's previous bidding history and the list of candidate items, " +
                                      "you must pick the best items for this specific user. " +
                                      "Always respond with a pure JSON object only, no extra text."
                        },
                        new
                        {
                            role = "user",
                            content = "Here is the user context and candidate items in JSON format. " +
                                      "Return JSON: { \"itemIds\": [<itemId1>, <itemId2>, ...] } with at most 'limit' items.\n\n" +
                                      userPromptJson
                        }
                    },
                    temperature = 0.3,
                    response_format = new
                    {
                        type = "json_object"
                    }
                };

                var json = JsonSerializer.Serialize(payload);
                using var request = new HttpRequestMessage(HttpMethod.Post, "/v1/chat/completions")
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };

                using var response = await client.SendAsync(request, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    var errorText = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogWarning("OpenAI API returned non-success status {Status}: {Body}", response.StatusCode, errorText);
                    return candidateItems.Take(limit).ToList();
                }

                using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var document = await JsonDocument.ParseAsync(contentStream, cancellationToken: cancellationToken);

                var root = document.RootElement;
                var choices = root.GetProperty("choices");
                if (choices.GetArrayLength() == 0)
                {
                    return candidateItems.Take(limit).ToList();
                }

                var firstChoice = choices[0];
                var message = firstChoice.GetProperty("message");
                var content = message.GetProperty("content").GetString();
                if (string.IsNullOrWhiteSpace(content))
                {
                    return candidateItems.Take(limit).ToList();
                }

                OpenAiRecommendationResponse? parsed;
                try
                {
                    parsed = JsonSerializer.Deserialize<OpenAiRecommendationResponse>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse OpenAI recommendation response. Content: {Content}", content);
                    return candidateItems.Take(limit).ToList();
                }

                if (parsed == null || parsed.ItemIds == null || parsed.ItemIds.Count == 0)
                {
                    return candidateItems.Take(limit).ToList();
                }

                var idSet = new HashSet<int>(parsed.ItemIds);
                var selected = candidateItems.Where(i => idSet.Contains(i.Id)).Take(limit).ToList();

                // Nếu AI trả về id không trùng khớp, fallback thêm các item còn thiếu
                if (selected.Count < limit)
                {
                    var remaining = candidateItems
                        .Where(i => !selected.Any(s => s.Id == i.Id))
                        .Take(limit - selected.Count);
                    selected.AddRange(remaining);
                }

                return selected;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while calling OpenAI for recommendations. Falling back to general items.");
                return candidateItems.Take(limit).ToList();
            }
        }
    }
}


