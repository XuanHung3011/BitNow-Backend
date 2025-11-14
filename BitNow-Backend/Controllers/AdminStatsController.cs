using BitNow_Backend.BLL.IServices;
using Microsoft.AspNetCore.Mvc;

namespace BitNow_Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdminStatsController : ControllerBase
{
    private readonly IAdminStatsService _adminStatsService;
    private readonly ILogger<AdminStatsController> _logger;

    public AdminStatsController(IAdminStatsService adminStatsService, ILogger<AdminStatsController> logger)
    {
        _adminStatsService = adminStatsService;
        _logger = logger;
    }

    /// <summary>
    /// Get admin statistics
    /// </summary>
    [HttpGet]
    public async Task<ActionResult> GetAdminStats()
    {
        try
        {
            var stats = await _adminStatsService.GetAdminStatsAsync();
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting admin stats");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get detailed statistics for a specific type (users, auctions, revenue)
    /// </summary>
    [HttpGet("detail/{type}")]
    public async Task<ActionResult> GetAdminStatsDetail(string type)
    {
        try
        {
            var detail = await _adminStatsService.GetAdminStatsDetailAsync(type);
            return Ok(detail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting admin stats detail for type: {Type}", type);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}

