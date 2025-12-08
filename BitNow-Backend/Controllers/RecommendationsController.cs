using BitNow_Backend.BLL.IServices;
using BitNow_Backend.BLL.Services;
using BitNow_Backend.DAL.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace BitNow_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RecommendationsController : ControllerBase
    {
        private readonly IRecommendationService _recommendationService;
        private readonly IVectorSyncService _vectorSyncService;
        private readonly ILogger<RecommendationsController> _logger;

        public RecommendationsController(
            IRecommendationService recommendationService,
            IVectorSyncService vectorSyncService,
            ILogger<RecommendationsController> logger)
        {
            _recommendationService = recommendationService;
            _vectorSyncService = vectorSyncService;
            _logger = logger;
        }


        /// API gợi ý "Dành riêng cho bạn" cho người dùng, sử dụng vector similarity search với Pinecone.
        [HttpGet("personalized")]
        [ProducesResponseType(typeof(IEnumerable<ItemResponseDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<ItemResponseDto>>> GetPersonalized(
            [FromQuery] int userId,
            [FromQuery] int limit = 4,
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
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid request for recommendations: {Message}", ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Error getting personalized recommendations for user {UserId}: {Message}", userId, ex.Message);
                return StatusCode(500, new { message = ex.Message, error = "Recommendation service error" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error getting personalized recommendations for user {UserId}", userId);
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }
        [HttpPost("sync/active-auctions")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> SyncActiveAuctions(CancellationToken cancellationToken)
        {
            try
            {
                await _vectorSyncService.SyncActiveAuctionsAsync(cancellationToken);
                return Ok(new { message = "Successfully synced active auctions to Pinecone" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing active auctions");
                return StatusCode(500, new { message = "Sync failed", error = ex.Message });
            }
        }


        /// Xóa các auctions đã hết hạn khỏi Pinecone
        [HttpPost("sync/remove-expired")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> RemoveExpiredAuctions(CancellationToken cancellationToken)
        {
            try
            {
                await _vectorSyncService.RemoveExpiredAuctionsAsync(cancellationToken);
                return Ok(new { message = "Successfully removed expired auctions from Pinecone" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing expired auctions");
                return StatusCode(500, new { message = "Remove failed", error = ex.Message });
            }
        }

        [HttpPost("sync/clear-all")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ClearAllVectors(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogWarning("Clearing ALL vectors from Pinecone");

                await _vectorSyncService.ClearAllVectorsAsync(cancellationToken);

                return Ok(new
                {
                    message = "Successfully deleted ALL vectors from Pinecone",
                    note = "Run /sync/active-auctions to rebuild recommendation data"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing all vectors");
                return StatusCode(500, new { message = "Clear operation failed", error = ex.Message });
            }
        }
    }
}

