using BitNow_Backend.BLL.IServices;
using BitNow_Backend.DAL.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace BitNow_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ItemsController : ControllerBase
    {
        private readonly IItemService _itemService;
        private readonly ILogger<ItemsController> _logger;

        public ItemsController(IItemService itemService, ILogger<ItemsController> logger)
        {
            _itemService = itemService;
            _logger = logger;
        }

        /// <summary>
        /// Get all items with pagination, filtering by status and category, and sorting
        /// </summary>
        /// <param name="statuses">Filter by status: 'pending', 'approved', 'rejected', 'archived' (comma-separated for multiple)</param>
        /// <param name="categoryId">Filter by category ID</param>
        /// <param name="sortBy">Sort by: 'Title', 'BasePrice', 'CreatedAt' (default: 'CreatedAt')</param>
        /// <param name="sortOrder">Sort order: 'asc' or 'desc' (default: 'desc')</param>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="pageSize">Page size (default: 10, max: 100)</param>
        [HttpGet]
        public async Task<ActionResult<PaginatedResult<ItemResponseDto>>> GetAllItems(
            [FromQuery] string? statuses = null,
            [FromQuery] int? categoryId = null,
            [FromQuery] string? sortBy = "CreatedAt",
            [FromQuery] string? sortOrder = "desc",
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                // Validate parameters
                if (page < 1) page = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 10;
                if (string.IsNullOrEmpty(sortBy)) sortBy = "CreatedAt";
                if (string.IsNullOrEmpty(sortOrder)) sortOrder = "desc";

                // Validate sortBy values
                var validSortBy = new[] { "Title", "BasePrice", "CreatedAt" };
                if (!validSortBy.Contains(sortBy, StringComparer.OrdinalIgnoreCase))
                {
                    return BadRequest(new { message = $"sortBy must be one of: {string.Join(", ", validSortBy)}" });
                }

                // Validate sortOrder values
                if (!string.Equals(sortOrder, "asc", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(sortOrder, "desc", StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest(new { message = "sortOrder must be 'asc' or 'desc'" });
                }

                // Parse statuses from comma-separated string
                List<string>? statusList = null;
                if (!string.IsNullOrWhiteSpace(statuses))
                {
                    statusList = statuses.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim())
                        .Where(s => !string.IsNullOrWhiteSpace(s))
                        .ToList();

                    // Validate status values
                    var validStatuses = new[] { "pending", "approved", "rejected", "archived" };
                    var invalidStatuses = statusList.Where(s => !validStatuses.Contains(s, StringComparer.OrdinalIgnoreCase)).ToList();
                    if (invalidStatuses.Any())
                    {
                        return BadRequest(new { message = $"Invalid status values: {string.Join(", ", invalidStatuses)}. Valid values are: {string.Join(", ", validStatuses)}" });
                    }
                }

                var filter = new ItemFilterAllDto
                {
                    Statuses = statusList,
                    CategoryId = categoryId,
                    SortBy = sortBy,
                    SortOrder = sortOrder,
                    Page = page,
                    PageSize = pageSize
                };

                var result = await _itemService.GetAllItemsWithFilterAsync(filter);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all items");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Approve an item (change status to 'approved')
        /// </summary>
        /// <param name="id">Item ID</param>
        [HttpPut("{id}/approve")]
        public async Task<ActionResult> ApproveItem(int id)
        {
            try
            {
                var result = await _itemService.ApproveItemAsync(id);
                if (!result)
                {
                    return NotFound(new { message = $"Item with ID {id} not found" });
                }

                return Ok(new { message = "Item approved successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving item {ItemId}", id);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Reject an item (change status to 'rejected')
        /// </summary>
        /// <param name="id">Item ID</param>
        [HttpPut("{id}/reject")]
        public async Task<ActionResult> RejectItem(int id)
        {
            try
            {
                var result = await _itemService.RejectItemAsync(id);
                if (!result)
                {
                    return NotFound(new { message = $"Item with ID {id} not found" });
                }

                return Ok(new { message = "Item rejected successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting item {ItemId}", id);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }
    }
}

