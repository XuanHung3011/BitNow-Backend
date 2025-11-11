using BitNow_Backend.BLL.IServices;
using Microsoft.AspNetCore.Mvc;

namespace BitNow_Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PlatformAnalyticsController : ControllerBase
{
    private readonly IPlatformAnalyticsService _platformAnalyticsService;
    private readonly ILogger<PlatformAnalyticsController> _logger;

    public PlatformAnalyticsController(
        IPlatformAnalyticsService platformAnalyticsService,
        ILogger<PlatformAnalyticsController> logger)
    {
        _platformAnalyticsService = platformAnalyticsService;
        _logger = logger;
    }

    /// <summary>
    /// Get platform analytics data
    /// </summary>
    [HttpGet]
    public async Task<ActionResult> GetPlatformAnalytics()
    {
        try
        {
            var analytics = await _platformAnalyticsService.GetPlatformAnalyticsAsync();
            return Ok(analytics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting platform analytics");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}

