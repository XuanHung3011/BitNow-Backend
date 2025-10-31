using BitNow_Backend.BLL.IServices;
using BitNow_Backend.DAL.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace BitNow_Backend.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class WatchlistController : ControllerBase
	{
		private readonly IWatchlistService _watchlistService;

		public WatchlistController(IWatchlistService watchlistService)
		{
			_watchlistService = watchlistService;
		}

		/// <summary>
		/// Add an auction to user's watchlist
		/// </summary>
		[HttpPost("add")]
		public async Task<ActionResult> AddWatchList([FromBody] AddToWatchlistRequest request)
		{
			if (!ModelState.IsValid) return BadRequest(ModelState);
			var ok = await _watchlistService.AddAsync(request);
			return ok ? Ok(new { message = "Added" }) : BadRequest("Failed to add");
		}

		/// <summary>
		/// Remove an auction from user's watchlist
		/// </summary>
		[HttpPost("remove")]
		public async Task<ActionResult> RemoveWatchList([FromBody] RemoveFromWatchlistRequest request)
		{
			if (!ModelState.IsValid) return BadRequest(ModelState);
			var ok = await _watchlistService.RemoveAsync(request);
			return ok ? Ok(new { message = "Removed" }) : NotFound("Not found");
		}

		/// <summary>
		/// Get user's watchlist details
		/// </summary>
		[HttpGet("user/{userId}")]
		public async Task<ActionResult<IEnumerable<WatchlistItemDto>>> GetByUser(int userId)
		{
			var items = await _watchlistService.GetByUserAsync(userId);
			return Ok(items);
		}

		/// <summary>
		/// Get a specific watchlist record by its id
		/// </summary>
		[HttpGet("detail/{watchlistId}")]
		public async Task<ActionResult<WatchlistItemDto>> GetDetail(int watchlistId)
		{
			var item = await _watchlistService.GetDetailAsync(watchlistId);
			if (item == null) return NotFound();
			return Ok(item);
		}

		/// <summary>
		/// Get a user's watchlist item by auction id
		/// </summary>
		[HttpGet("user/{userId}/auction/{auctionId}")]
		public async Task<ActionResult<WatchlistItemDto>> GetDetailByUserAuction(int userId, int auctionId)
		{
			var item = await _watchlistService.GetDetailByUserAuctionAsync(userId, auctionId);
			if (item == null) return NotFound();
			return Ok(item);
		}
	}
}
