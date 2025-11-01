using BitNow_Backend.BLL.IServices;
using BitNow_Backend.DAL.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace BitNow_Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RatingsController : ControllerBase
{
    private readonly IRatingService _ratingService;
    private readonly ILogger<RatingsController> _logger;

    public RatingsController(IRatingService ratingService, ILogger<RatingsController> logger)
    {
        _ratingService = ratingService;
        _logger = logger;
    }

    /// <summary>
    /// Submit rating and feedback between seller and winner after auction completion
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<RatingResponseDto>> Create([FromBody] RatingCreateDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _ratingService.CreateAsync(dto);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating rating for auction {AuctionId}", dto.AuctionId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get ratings received by a user
    /// </summary>
    [HttpGet("user/{userId}")]
    public async Task<ActionResult<IEnumerable<RatingResponseDto>>> GetForUser(int userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        try
        {
            var result = await _ratingService.GetForUserAsync(userId, page, pageSize);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting ratings for user {UserId}", userId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get ratings for an auction
    /// </summary>
    [HttpGet("auction/{auctionId}")]
    public async Task<ActionResult<IEnumerable<RatingResponseDto>>> GetForAuction(int auctionId)
    {
        try
        {
            var result = await _ratingService.GetForAuctionAsync(auctionId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting ratings for auction {AuctionId}", auctionId);
            return StatusCode(500, "Internal server error");
        }
    }
}


