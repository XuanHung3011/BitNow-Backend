using BitNow_Backend.BLL.IServices;
using Microsoft.AspNetCore.Mvc;

namespace BitNow_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VectorSyncController : ControllerBase
    {
        private readonly IVectorSyncService _vectorSyncService;
        private readonly ILogger<VectorSyncController> _logger;

        public VectorSyncController(
            IVectorSyncService vectorSyncService,
            ILogger<VectorSyncController> logger)
        {
            _vectorSyncService = vectorSyncService;
            _logger = logger;
        }

        /// <summary>
        /// Đồng bộ tất cả các phiên đấu giá đang active vào Pinecone vector database.
        /// </summary>
        [HttpPost("sync-active")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> SyncActiveAuctions(CancellationToken cancellationToken = default)
        {
            try
            {
                await _vectorSyncService.SyncActiveAuctionsAsync(cancellationToken);
                return Ok(new { message = "Active auctions synced successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing active auctions to Pinecone");
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        /// <summary>
        /// Xóa các phiên đấu giá đã hết thời gian khỏi Pinecone vector database.
        /// </summary>
        [HttpPost("remove-expired")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> RemoveExpiredAuctions(CancellationToken cancellationToken = default)
        {
            try
            {
                await _vectorSyncService.RemoveExpiredAuctionsAsync(cancellationToken);
                return Ok(new { message = "Expired auctions removed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing expired auctions from Pinecone");
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }
    }
}

