using BitNow_Backend.BLL.IServices;
using BitNow_Backend.DAL.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace BitNow_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HomeController : ControllerBase
    {
        private readonly IItemService _itemService;
        private readonly ILogger<HomeController> _logger;

        public HomeController(IItemService itemService, ILogger<HomeController> logger)
        {
            _itemService = itemService;
            _logger = logger;
        }

        /// <summary>
        /// Get all approved items (no pagination)
        /// </summary>
        [HttpGet("items")]
        public async Task<ActionResult<IEnumerable<ItemResponseDto>>> GetAllItems()
        {
            try
            {
                var items = await _itemService.GetAllApprovedItemsAsync();
                return Ok(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all approved items");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get all approved items with pagination
        /// </summary>
        [HttpGet("items/paged")]
        public async Task<ActionResult<object>> GetAllItemsPaged(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                if (page < 1) page = 1;
                if (pageSize < 1) pageSize = 10;
                if (pageSize > 100) pageSize = 100;

                var (items, totalCount) = await _itemService.GetAllApprovedItemsWithCountAsync(page, pageSize);

                var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

                return Ok(new
                {
                    items,
                    pagination = new
                    {
                        currentPage = page,
                        pageSize,
                        totalCount,
                        totalPages,
                        hasNextPage = page < totalPages,
                        hasPreviousPage = page > 1
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting paged approved items");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Search approved items (no pagination)
        /// </summary>
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<ItemResponseDto>>> SearchItems(
            [FromQuery] string searchTerm)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                {
                    return BadRequest(new { message = "Search term is required" });
                }

                var items = await _itemService.SearchApprovedItemsAsync(searchTerm);
                return Ok(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching approved items with term: {SearchTerm}", searchTerm);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Search approved items with pagination
        /// </summary>
        [HttpGet("search/paged")]
        public async Task<ActionResult<object>> SearchItemsPaged(
            [FromQuery] string searchTerm,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                {
                    return BadRequest(new { message = "Search term is required" });
                }

                if (page < 1) page = 1;
                if (pageSize < 1) pageSize = 10;
                if (pageSize > 100) pageSize = 100;

                var (items, totalCount) = await _itemService.SearchApprovedItemsWithCountAsync(searchTerm, page, pageSize);

                var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

                return Ok(new
                {
                    items,
                    searchTerm,
                    pagination = new
                    {
                        currentPage = page,
                        pageSize,
                        totalCount,
                        totalPages,
                        hasNextPage = page < totalPages,
                        hasPreviousPage = page > 1
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching paged approved items with term: {SearchTerm}", searchTerm);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }
        [HttpPost("filter")]
        public async Task<ActionResult<object>> FilterItems(
        [FromBody] ItemFilterDto filter,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
        {
            try
            {
                if (page < 1) page = 1;
                if (pageSize < 1) pageSize = 10;
                if (pageSize > 100) pageSize = 100;

                var (items, totalCount) = await _itemService.FilterApprovedItemsAsync(filter, page, pageSize);

                var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

                return Ok(new
                {
                    items,
                    filter,
                    pagination = new
                    {
                        currentPage = page,
                        pageSize,
                        totalCount,
                        totalPages,
                        hasNextPage = page < totalPages,
                        hasPreviousPage = page > 1
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error filtering approved items");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }
        [HttpGet("categories")]
        public async Task<ActionResult<IEnumerable<CategoryDto>>> GetCategories()
        {
            try
            {
                var categories = await _itemService.GetCategoriesAsync();
                return Ok(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting categories");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }
    }
}
