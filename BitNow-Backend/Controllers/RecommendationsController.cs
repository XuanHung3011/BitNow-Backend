using BitNow_Backend.BLL.IServices;
using BitNow_Backend.DAL.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace BitNow_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RecommendationsController : ControllerBase
    {
        private readonly IRecommendationService _recommendationService;
        private readonly ILogger<RecommendationsController> _logger;

        public RecommendationsController(
            IRecommendationService recommendationService,
            ILogger<RecommendationsController> logger)
        {
            _recommendationService = recommendationService;
            _logger = logger;
        }

        /// <summary>
        /// API gợi ý "Dành riêng cho bạn" cho người dùng, sử dụng OpenAI + dữ liệu hệ thống.
        /// </summary>
        /// <param name="userId">Id người dùng</param>
        /// <param name="limit">Số lượng item cần gợi ý</param>
        [HttpGet("personalized")]
        [ProducesResponseType(typeof(IEnumerable<ItemResponseDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<ItemResponseDto>>> GetPersonalized(
            [FromQuery] int userId,
            [FromQuery] int limit = 8,
            CancellationToken cancellationToken = default)
        {
            if (userId <= 0)
            {
                return BadRequest(new { message = "userId is required and must be greater than 0" });
            }

            try
            {
                var items = await _recommendationService.GetPersonalizedItemsAsync(userId, limit, cancellationToken);
                return Ok(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting personalized recommendations for user {UserId}", userId);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }
    }
}

