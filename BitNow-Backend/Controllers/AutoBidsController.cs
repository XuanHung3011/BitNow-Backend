using BitNow_Backend.BLL.IServices;
using BitNow_Backend.DAL.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace BitNow_Backend.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class AutoBidsController : ControllerBase
	{
		private readonly IAutoBidService _autoBidService;
		private readonly ILogger<AutoBidsController> _logger;

		public AutoBidsController(IAutoBidService autoBidService, ILogger<AutoBidsController> logger)
		{
			_autoBidService = autoBidService;
			_logger = logger;
		}

		/// <summary>
		/// Tạo hoặc cập nhật auto bid cho một phiên đấu giá
		/// </summary>
		[HttpPost]
		public async Task<ActionResult<AutoBidDto>> CreateOrUpdate([FromBody] CreateAutoBidDto dto)
		{
			try
			{
				if (dto == null || dto.MaxAmount <= 0)
				{
					return BadRequest(new { message = "Invalid request" });
				}

				var result = await _autoBidService.CreateOrUpdateAutoBidAsync(dto.AuctionId, dto.UserId, dto.MaxAmount);
				return Ok(result);
			}
			catch (InvalidOperationException ex)
			{
				return BadRequest(new { message = ex.Message });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error creating/updating auto bid");
				return StatusCode(500, new { message = "Internal server error" });
			}
		}

		/// <summary>
		/// Lấy thông tin auto bid của user cho một phiên đấu giá
		/// </summary>
		[HttpGet("auction/{auctionId}/user/{userId}")]
		public async Task<ActionResult<AutoBidDto>> Get(int auctionId, int userId)
		{
			try
			{
				var result = await _autoBidService.GetAutoBidAsync(auctionId, userId);
				if (result == null)
				{
					return NotFound(new { message = "Auto bid not found" });
				}
				return Ok(result);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting auto bid");
				return StatusCode(500, new { message = "Internal server error" });
			}
		}

		/// <summary>
		/// Hủy auto bid
		/// </summary>
		[HttpDelete("auction/{auctionId}/user/{userId}")]
		public async Task<ActionResult> Deactivate(int auctionId, int userId)
		{
			try
			{
				var result = await _autoBidService.DeactivateAutoBidAsync(auctionId, userId);
				if (!result)
				{
					return NotFound(new { message = "Auto bid not found" });
				}
				return Ok(new { message = "Auto bid deactivated successfully" });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error deactivating auto bid");
				return StatusCode(500, new { message = "Internal server error" });
			}
		}

		/// <summary>
		/// Lấy bước nhảy giá dựa trên giá hiện tại
		/// </summary>
		[HttpGet("increment/{currentPrice}")]
		public ActionResult<decimal> GetBidIncrement(decimal currentPrice)
		{
			try
			{
				var increment = _autoBidService.CalculateBidIncrement(currentPrice);
				return Ok(new { increment });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error calculating bid increment");
				return StatusCode(500, new { message = "Internal server error" });
			}
		}
	}
}

