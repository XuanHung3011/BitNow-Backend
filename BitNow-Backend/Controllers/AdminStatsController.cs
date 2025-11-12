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
}

