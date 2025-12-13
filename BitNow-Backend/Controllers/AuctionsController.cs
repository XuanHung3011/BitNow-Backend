using BitNow_Backend.BLL.IServices;
using BitNow_Backend.DAL.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using BitNow_Backend.RealTime;
using BitNow_Backend.BLL.Services;
using BitNow_Backend.BLL.IServices;

namespace BitNow_Backend.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class AuctionsController : ControllerBase
	{
		private readonly IAuctionService _auctionService;
		private readonly IBidService _bidService;
		private readonly IHubContext<AuctionHub> _hubContext;
        private readonly ILogger<AuctionsController> _logger;
        private readonly IVectorSyncService _vectorSyncService;  
        private readonly IItemService _itemService;
        private readonly IUserAuctionViewService _userAuctionViewService;


        public AuctionsController(
            IAuctionService auctionService,
            IBidService bidService,
            IHubContext<AuctionHub> hubContext,
            ILogger<AuctionsController> logger,
            IVectorSyncService vectorSyncService,
            IItemService itemService,
            IUserAuctionViewService userAuctionViewService)
        {
            _auctionService = auctionService;
            _bidService = bidService;
            _hubContext = hubContext;
            _logger = logger;
            _vectorSyncService = vectorSyncService;  
            _itemService = itemService;
            _userAuctionViewService = userAuctionViewService;
        }

        /// <summary>
		/// Create a new auction (status will be 'active' immediately)
		/// </summary>
		[HttpPost]
        public async Task<ActionResult<AuctionResponseDto>> Create([FromBody] CreateAuctionDto dto)
        {
            try
            {
                _logger.LogInformation("Create auction called with ItemId: {ItemId}, SellerId: {SellerId}", dto?.ItemId, dto?.SellerId);

                if (dto == null)
                {
                    _logger.LogWarning("CreateAuctionDto is null");
                    return BadRequest(new { message = "Request body is required" });
                }

                var result = await _auctionService.CreateAuctionAsync(dto);
                if (result == null)
                {
                    _logger.LogWarning("CreateAuctionAsync returned null");
                    return BadRequest(new { message = "Failed to create auction" });
                }

                _logger.LogInformation("Auction created successfully with ID: {AuctionId}", result.Id);

                //  THÊM: Sync auction mới vào Pinecone
                try
                {
                    var itemDto = await _itemService.GetByIdAsync(dto.ItemId);
                    if (itemDto != null)
                    {
                        await _vectorSyncService.SyncAuctionAsync(itemDto);
                        _logger.LogInformation("Successfully synced auction {AuctionId} to Pinecone", result.Id);
                    }
                }
                catch (Exception ex)
                {
                    // Log lỗi nhưng không làm fail request tạo auction
                    _logger.LogWarning(ex, "Failed to sync auction {AuctionId} to Pinecone, but auction was created successfully", result.Id);
                }

                var payload = new
                {
                    auctionId = result.Id,
                    status = result.Status ?? "active",
                    winnerId = (int?)null,
                    finalPrice = (decimal?)null,
                    completionType = "status-change",
                    timestamp = DateTime.Now
                };
                await _hubContext.Clients.Group(AuctionHub.AdminAuctionsGroup).SendAsync("AdminAuctionStatusUpdated", payload);
                await _hubContext.Clients.Group(AuctionHub.AdminDashboardGroup).SendAsync("AdminStatsUpdated");
                await _hubContext.Clients.Group(AuctionHub.AdminAnalyticsGroup).SendAsync("AdminAnalyticsUpdated");

                return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument when creating auction: {Message}", ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation when creating auction: {Message}", ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access when creating auction: {Message}", ex.Message);
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating auction: {Message}, StackTrace: {StackTrace}, InnerException: {InnerException}",
                    ex.Message, ex.StackTrace, ex.InnerException?.Message);

                // Return more detailed error message
                var errorMessage = ex.Message;
                if (ex.InnerException != null)
                {
                    errorMessage += $" Inner: {ex.InnerException.Message}";
                }

                return StatusCode(500, new { message = "Internal server error", error = errorMessage });
            }
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<AuctionDetailDto>> Get(
            int id,
            [FromQuery] int? userId = null)
		{
			var dto = await _auctionService.GetDetailAsync(id);
			if (dto == null) return NotFound();

            if (userId.HasValue && userId.Value > 0)
            {
                await _userAuctionViewService.LogViewAsync(userId.Value, id);
            }

            return Ok(dto);
		}

		[HttpPost("{id}/bid")]
		public async Task<ActionResult<BidResultDto>> PlaceBid(int id, [FromBody] BidRequestDto request)
		{
			try
			{
				var result = await _bidService.PlaceBidAsync(id, request.BidderId, request.Amount);
				// Broadcast đã được xử lý trong BidService thông qua IBidNotificationService
				// Không cần broadcast lại ở đây để tránh duplicate
				return Ok(result);
			}
			catch (InvalidOperationException ex)
			{
				return BadRequest(new { message = ex.Message });
			}
		}

        [HttpPost("{id:int}/buy-now")]
        public async Task<ActionResult<AuctionCompletionResultDto>> BuyNow(int id, [FromBody] BuyNowRequestDto request)
        {
            if (request == null || request.BuyerId <= 0)
            {
                return BadRequest(new { message = "BuyerId is required" });
            }

            try
            {
                var result = await _auctionService.BuyNowAsync(id, request.BuyerId);

                var payload = new
                {
                    auctionId = result.AuctionId,
                    status = result.Status,
                    winnerId = result.WinnerId,
                    finalPrice = result.FinalPrice,
                    completionType = result.CompletionType,
                    timestamp = result.CompletedAt
                };

                await _hubContext.Clients.Group($"auction-{id}").SendAsync("AuctionStatusUpdated", payload);
                await _hubContext.Clients.Group(AuctionHub.AdminAuctionsGroup).SendAsync("AdminAuctionStatusUpdated", payload);
                await _hubContext.Clients.Group(AuctionHub.AdminDashboardGroup).SendAsync("AdminStatsUpdated");
                await _hubContext.Clients.Group(AuctionHub.AdminAnalyticsGroup).SendAsync("AdminAnalyticsUpdated");

                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error triggering buy-now for auction {AuctionId}", id);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

		[HttpGet("{id}/bids/recent")]
		public async Task<ActionResult<IReadOnlyList<BidDto>>> GetRecentBids(int id, [FromQuery] int limit = 100)
		{
			limit = Math.Clamp(limit, 1, 100);
			var bids = await _bidService.GetRecentBidsAsync(id, limit);
			return Ok(bids);
		}

		[HttpGet("{id}/bids/highest")]
		public async Task<ActionResult<decimal?>> GetHighestBid(int id)
		{
			var highest = await _bidService.GetHighestBidAsync(id);
			return Ok(highest);
		}


        [HttpGet("buyer/{bidderId}/active")]
        public async Task<ActionResult<PaginatedResult<BuyerActiveBidDto>>> GetActiveBidsByBuyer(
            int bidderId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var result = await _auctionService.GetActiveBidsByBuyerAsync(bidderId, page, pageSize);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active bids for buyer {BidderId}", bidderId);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpGet("buyer/{bidderId}/won")]
        public async Task<ActionResult<PaginatedResult<BuyerWonAuctionDto>>> GetWonAuctionsByBuyer(
            int bidderId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var result = await _auctionService.GetWonAuctionsByBuyerAsync(bidderId, page, pageSize);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting won auctions for buyer {BidderId}", bidderId);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpGet("buyer/{bidderId}/history")]
        public async Task<ActionResult<PaginatedResultB<BiddingHistoryDto>>> GetBiddingHistory(
            int bidderId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                _logger.LogInformation("Getting bidding history for buyer {BidderId}, page {Page}, pageSize {PageSize}",
                    bidderId, page, pageSize);

                var result = await _bidService.GetBiddingHistoryAsync(bidderId, page, pageSize);

                _logger.LogInformation("Successfully retrieved {Count} bids for buyer {BidderId}",
                    result.Data.Count, bidderId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting bidding history for buyer {BidderId}: {Message}",
                    bidderId, ex.Message);
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        /// <summary>
        /// Get auctions by seller ID
        /// </summary>
        [HttpGet("seller/{sellerId}")]
        public async Task<ActionResult<List<SellerAuctionDto>>> GetAuctionsBySeller(int sellerId)
        {
            try
            {
                var result = await _auctionService.GetAuctionsBySellerAsync(sellerId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting auctions for seller {SellerId}", sellerId);
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        /// <summary>
        /// Get all auctions with pagination, search, and filtering (Public endpoint)
        /// </summary>
        /// <param name="searchTerm">Search by item title or seller name</param>
        /// <param name="statuses">Filter by status: 'active', 'scheduled', 'completed', 'cancelled' (comma-separated for multiple)</param>
        /// <param name="categoryId">Filter by category ID</param>
        /// <param name="sortBy">Sort by: 'ItemTitle', 'EndTime', 'CurrentBid', 'BidCount' (default: 'EndTime')</param>
        /// <param name="sortOrder">Sort order: 'asc' or 'desc' (default: 'desc')</param>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="pageSize">Page size (default: 10, max: 100)</param>
        [HttpGet]
        public async Task<ActionResult<PaginatedResult<AuctionListItemDto>>> GetAllAuctions(
            [FromQuery] string? searchTerm = null,
            [FromQuery] string? statuses = null,
            [FromQuery] int? categoryId = null,
            [FromQuery] string? sortBy = "EndTime",
            [FromQuery] string? sortOrder = "desc",
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                // Validate parameters
                if (page < 1) page = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 10;
                if (string.IsNullOrEmpty(sortBy)) sortBy = "EndTime";
                if (string.IsNullOrEmpty(sortOrder)) sortOrder = "desc";

                // Validate sortBy values
                var validSortBy = new[] { "ItemTitle", "EndTime", "CurrentBid", "BidCount" };
                if (!validSortBy.Contains(sortBy, StringComparer.OrdinalIgnoreCase))
                {
                    return BadRequest(new { message = $"sortBy must be one of: {string.Join(", ", validSortBy)}" });
                }

                // Validate sortOrder values
                if (!string.Equals(sortOrder, "asc", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(sortOrder, "desc", StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest(new { message = "sortOrder must be 'asc' or 'desc'" });
                }

                // Parse statuses from comma-separated string
                List<string>? statusList = null;
                if (!string.IsNullOrWhiteSpace(statuses))
                {
                    statusList = statuses.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim())
                        .Where(s => !string.IsNullOrWhiteSpace(s))
                        .ToList();

                    // Validate status values
                    var validStatuses = new[] { "active", "scheduled", "completed", "cancelled" };
                    var invalidStatuses = statusList.Where(s => !validStatuses.Contains(s, StringComparer.OrdinalIgnoreCase)).ToList();
                    if (invalidStatuses.Any())
                    {
                        return BadRequest(new { message = $"Invalid status values: {string.Join(", ", invalidStatuses)}. Valid values are: {string.Join(", ", validStatuses)}" });
                    }
                }

                var filter = new AuctionFilterDto
                {
                    SearchTerm = searchTerm,
                    Statuses = statusList,
                    CategoryId = categoryId,
                    SortBy = sortBy,
                    SortOrder = sortOrder,
                    Page = page,
                    PageSize = pageSize
                };

                var result = await _auctionService.GetAuctionsWithFilterAsync(filter);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all auctions");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

    }
}
