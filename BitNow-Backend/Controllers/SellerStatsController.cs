using BitNow_Backend.BLL.IServices;
using Microsoft.AspNetCore.Mvc;

namespace BitNow_Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SellerStatsController : ControllerBase
{
    private readonly ISellerStatsService _sellerStatsService;
    private readonly ILogger<SellerStatsController> _logger;

    public SellerStatsController(ISellerStatsService sellerStatsService, ILogger<SellerStatsController> logger)
    {
        _sellerStatsService = sellerStatsService;
        _logger = logger;
    }

    /// <summary>
    /// Get seller statistics
    /// </summary>
    [HttpGet("{sellerId}")]
    public async Task<ActionResult> GetSellerStats(int sellerId)
    {
        try
        {
            var stats = await _sellerStatsService.GetSellerStatsAsync(sellerId);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting seller stats for seller {SellerId}", sellerId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get detailed statistics for a specific type (auctions, revenue, bids)
    /// </summary>
    [HttpGet("{sellerId}/detail/{type}")]
    public async Task<ActionResult> GetSellerStatsDetail(int sellerId, string type)
    {
        try
        {
            var detail = await _sellerStatsService.GetSellerStatsDetailAsync(sellerId, type);
            return Ok(detail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting seller stats detail for seller {SellerId}, type: {Type}", sellerId, type);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}

