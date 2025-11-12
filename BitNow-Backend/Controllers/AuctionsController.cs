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

		public AuctionsController(IAuctionService auctionService, IBidService bidService, IHubContext<AuctionHub> hubContext)
		{
			_auctionService = auctionService;
			_bidService = bidService;
			_hubContext = hubContext;
		}

		[HttpGet("{id}")]
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
	}
}
