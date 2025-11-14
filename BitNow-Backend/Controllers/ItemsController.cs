using BitNow_Backend.BLL.IServices;
using BitNow_Backend.DAL.DTOs;
using Microsoft.AspNetCore.Mvc;
using BitNow_Backend.Services;
using System;
using System.Linq;

namespace BitNow_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ItemsController : ControllerBase
    {
        private readonly IItemService _itemService;
        private readonly ILogger<ItemsController> _logger;
        private readonly IFileUploadService _fileUploadService;

        public ItemsController(IItemService itemService, IFileUploadService fileUploadService, ILogger<ItemsController> logger)
        {
            _itemService = itemService;
            _fileUploadService = fileUploadService;
            _logger = logger;
        }

        /// <summary>
        /// Create a new item (status will be 'pending' for admin approval)
        /// </summary>
        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<ItemResponseDto>> CreateItem()
        {
            try
            {
                // Manually read from Form to handle model binding issues
                var form = await Request.ReadFormAsync();

                // Parse CreateItemDto from form
                if (!int.TryParse(form["SellerId"].ToString(), out int sellerId) || sellerId <= 0)
                {
                    return BadRequest(new { message = "SellerId is required and must be greater than 0" });
                }

                if (!int.TryParse(form["CategoryId"].ToString(), out int categoryId) || categoryId <= 0)
                {
                    return BadRequest(new { message = "CategoryId is required and must be greater than 0" });
                }

                var title = form["Title"].ToString();
                if (string.IsNullOrWhiteSpace(title))
                {
                    return BadRequest(new { message = "Title is required" });
                }

                if (!decimal.TryParse(form["BasePrice"].ToString(), out decimal basePrice) || basePrice <= 0)
                {
                    return BadRequest(new { message = "BasePrice must be greater than 0" });
                }

                var dto = new CreateItemDto
                {
                    SellerId = sellerId,
                    CategoryId = categoryId,
                    Title = title,
                    Description = form["Description"].ToString(),
                    Condition = form["Condition"].ToString(),
                    Location = form["Location"].ToString(),
                    BasePrice = basePrice
                };

                _logger.LogInformation("CreateItem called with sellerId: {SellerId}, title: {Title}", dto.SellerId, dto.Title);

                // Get image files
                var images = form.Files.Where(f => f.Name == "images").ToList();

                // Handle image uploads
                string? imagesPath = null;
                if (images != null && images.Count > 0)
                {
                    try
                    {
                        // Truyền tên sản phẩm để đặt tên file theo tên sản phẩm
                        var savedPaths = await _fileUploadService.SaveImagesAsync(images, dto.Title);
                        imagesPath = string.Join(",", savedPaths);
                        _logger.LogInformation("Saved {Count} images for item '{Title}'", savedPaths.Count, dto.Title);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error saving images");
                        return BadRequest(new { message = $"Error saving images: {ex.Message}" });
                    }
                }

                var result = await _itemService.CreateItemAsync(dto, imagesPath);
                if (result == null)
                {
                    _logger.LogWarning("CreateItemAsync returned null");
                    return BadRequest(new { message = "Failed to create item" });
                }

                _logger.LogInformation("Item created successfully with ID: {ItemId}", result.Id);
                return Ok(result); // Use Ok instead of CreatedAtAction to ensure response is returned
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument when creating item");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating item");
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }


        /// <summary>
        /// Get all items with pagination, filtering by status, category, seller, and sorting
        /// </summary>
        /// <param name="statuses">Filter by status: 'pending', 'approved', 'rejected', 'archived' (comma-separated for multiple)</param>
        /// <param name="categoryId">Filter by category ID</param>
        /// <param name="sellerId">Filter by seller ID (quan trọng: chỉ hiển thị items của seller đó)</param>
        /// <param name="sortBy">Sort by: 'Title', 'BasePrice', 'CreatedAt' (default: 'CreatedAt')</param>
        /// <param name="sortOrder">Sort order: 'asc' or 'desc' (default: 'desc')</param>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="pageSize">Page size (default: 10, max: 100)</param>
        [HttpGet]
        public async Task<ActionResult<PaginatedResult<ItemResponseDto>>> GetAllItems(
            [FromQuery] string? statuses = null,
            [FromQuery] int? categoryId = null,
            [FromQuery] int? sellerId = null,
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
                    SellerId = sellerId,  // Quan trọng: filter theo sellerId để chỉ hiển thị items của seller đó
                    SortBy = sortBy,
                    SortOrder = sortOrder,
                    Page = page,
                    PageSize = pageSize
                };

                // Log để debug
                if (sellerId.HasValue)
                {
                    _logger.LogInformation("GetAllItems called with sellerId filter: {SellerId}", sellerId.Value);
                }

                var result = await _itemService.GetAllItemsWithFilterAsync(filter);
                
                // Log kết quả để debug
                if (sellerId.HasValue)
                {
                    _logger.LogInformation("GetAllItems returned {Count} items for sellerId: {SellerId}", result.Data.Count, sellerId.Value);
                }
                
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

        /// <summary>
        /// Get item by ID
        /// </summary>
        /// <param name="id">Item ID</param>
        [HttpGet("{id}")]
        public async Task<ActionResult<ItemResponseDto>> GetItemById(int id)
        {
            try
            {
                var item = await _itemService.GetByIdAsync(id);
                if (item == null)
                {
                    return NotFound(new { message = $"Item with ID {id} not found" });
                }

                return Ok(item);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting item {ItemId}", id);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }
    }
}

