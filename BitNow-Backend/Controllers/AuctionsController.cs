using BitNow_Backend.BLL.IServices;
using BitNow_Backend.DAL.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using BitNow_Backend.RealTime;

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

        public AuctionsController(IAuctionService auctionService, IBidService bidService, IHubContext<AuctionHub> hubContext, ILogger<AuctionsController> logger)
        {
            _auctionService = auctionService;
            _bidService = bidService;
            _hubContext = hubContext;
            _logger = logger;
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
        public async Task<ActionResult<AuctionDetailDto>> Get(int id)
		{
			var dto = await _auctionService.GetDetailAsync(id);
			if (dto == null) return NotFound();
			return Ok(dto);
		}

		[HttpPost("{id}/bid")]
		public async Task<ActionResult<BidResultDto>> PlaceBid(int id, [FromBody] BidRequestDto request)
		{
			try
			{
				var result = await _bidService.PlaceBidAsync(id, request.BidderId, request.Amount);
				// broadcast to group
				await _hubContext.Clients.Group($"auction-{id}")
					.SendAsync("BidPlaced", result);
				return Ok(result);
			}
			catch (InvalidOperationException ex)
			{
				return BadRequest(new { message = ex.Message });
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
    }
}
