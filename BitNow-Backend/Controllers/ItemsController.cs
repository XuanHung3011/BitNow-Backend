using BitNow_Backend.BLL.IServices;
using BitNow_Backend.DAL;
using BitNow_Backend.DAL.DTOs;
using Microsoft.AspNetCore.Mvc;
using BitNow_Backend.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using Microsoft.AspNetCore.SignalR;
using BitNow_Backend.RealTime;

namespace BitNow_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ItemsController : ControllerBase
    {
        private readonly IItemService _itemService;
        private readonly ILogger<ItemsController> _logger;
        private readonly IFileUploadService _fileUploadService;
        private readonly INotificationService _notificationService;
        private readonly BidNowDbContext _context;
        private readonly IHubContext<AuctionHub> _auctionHub;

        public ItemsController(
            IItemService itemService, 
            IFileUploadService fileUploadService, 
            ILogger<ItemsController> logger,
            INotificationService notificationService,
            BidNowDbContext context,
            IHubContext<AuctionHub> auctionHub)
        {
            _itemService = itemService;
            _fileUploadService = fileUploadService;
            _logger = logger;
            _notificationService = notificationService;
            _context = context;
            _auctionHub = auctionHub;
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

                // Tạo thông báo cho tất cả admin users về sản phẩm mới cần phê duyệt
                try
                {
                    _logger.LogInformation("Searching for admin users to notify about new item {ItemId}", result.Id);
                    
                    // Query trực tiếp từ UserRoles table và join với Users
                    var adminUserIds = await _context.UserRoles
                        .Where(ur => ur.Role.ToLower() == "admin")
                        .Select(ur => ur.UserId)
                        .Distinct()
                        .ToListAsync();

                    _logger.LogInformation("Found {Count} admin user IDs from UserRoles table", adminUserIds.Count);

                    if (adminUserIds.Count == 0)
                    {
                        _logger.LogWarning("No admin users found in UserRoles table for notification about item {ItemId}", result.Id);
                    }
                    else
                    {
                        // Lấy thông tin đầy đủ của admin users (chỉ những user active)
                        var adminUsers = await _context.Users
                            .Where(u => adminUserIds.Contains(u.Id) && (u.IsActive == null || u.IsActive == true))
                            .ToListAsync();

                        _logger.LogInformation("Found {Count} active admin users for notification about item {ItemId}", adminUsers.Count, result.Id);

                        if (adminUsers.Count == 0)
                        {
                            _logger.LogWarning("No active admin users found to notify about new item {ItemId}", result.Id);
                        }

                        var notificationCount = 0;
                        foreach (var admin in adminUsers)
                        {
                            try
                            {
                                // Truncate message nếu quá dài (max 500 chars)
                                var message = $"Sản phẩm mới '{dto.Title}' cần phê duyệt từ seller ID {dto.SellerId}";
                                if (message.Length > 500)
                                {
                                    message = message.Substring(0, 497) + "...";
                                }

                                _logger.LogInformation("Attempting to create notification for admin {AdminId} (Email: {Email}) about new item {ItemId}", 
                                    admin.Id, admin.Email, result.Id);

                                var notificationDto = new CreateNotificationDto
                                {
                                    UserId = admin.Id,
                                    Type = "item_pending",
                                    Message = message,
                                    Link = $"/admin?tab=pending"
                                };

                                var createdNotification = await _notificationService.CreateNotificationAsync(notificationDto);

                                notificationCount++;
                                _logger.LogInformation("Successfully created notification {NotificationId} for admin {AdminId} (Email: {Email}) about new item {ItemId}", 
                                    createdNotification.Id, admin.Id, admin.Email, result.Id);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Failed to create notification for admin {AdminId} (Email: {Email}) about item {ItemId}. Error: {Error}", 
                                    admin.Id, admin.Email, result.Id, ex.ToString());
                            }
                        }

                        _logger.LogInformation("Successfully created {Count}/{Total} notifications for admin users about new item {ItemId}", 
                            notificationCount, adminUsers.Count, result.Id);
                    }
                }
                catch (Exception ex)
                {
                    // Log error nhưng không fail request nếu notification creation fails
                    _logger.LogError(ex, "Failed to create notifications for admins about item {ItemId}. Error: {Error}", result.Id, ex.ToString());
                }

                // Gửi SignalR update để refresh admin dashboard real-time
                await NotifyPendingAndDashboardAsync(result.Id, "created");

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
                    var itemCount = result.Data?.Count() ?? 0;
                    _logger.LogInformation("GetAllItems returned {Count} items for sellerId: {SellerId}", itemCount, sellerId.Value);
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
                // Lấy thông tin item trước khi approve để lấy sellerId và title
                var item = await _itemService.GetByIdAsync(id);
                if (item == null)
                {
                    return NotFound(new { message = $"Item with ID {id} not found" });
                }

                var result = await _itemService.ApproveItemAsync(id);
                if (!result)
                {
                    return NotFound(new { message = $"Item with ID {id} not found" });
                }

                // Tạo thông báo cho seller về việc sản phẩm được phê duyệt
                try
                {
                    if (item.SellerId > 0)
                    {
                        // Truncate message nếu quá dài (max 500 chars)
                        var message = $"Sản phẩm '{item.Title}' của bạn đã được phê duyệt";
                        if (message.Length > 500)
                        {
                            message = message.Substring(0, 497) + "...";
                        }

                        await _notificationService.CreateNotificationAsync(new CreateNotificationDto
                        {
                            UserId = item.SellerId,
                            Type = "item_approved",
                            Message = message,
                            Link = $"/seller/items"
                        });

                        _logger.LogInformation("Created notification for seller {SellerId} about approved item {ItemId} (Title: {Title})", item.SellerId, id, item.Title);
                    }
                    else
                    {
                        _logger.LogWarning("Cannot create notification for approved item {ItemId}: SellerId is invalid ({SellerId})", id, item.SellerId);
                    }
                }
                catch (Exception ex)
                {
                    // Log error nhưng không fail request nếu notification creation fails
                    _logger.LogError(ex, "Failed to create notification for seller {SellerId} about approved item {ItemId}: {Message}", item.SellerId, id, ex.Message);
                }

                // Gửi SignalR update để refresh admin dashboard real-time
                await NotifyPendingAndDashboardAsync(id, "approved");

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
                // Lấy thông tin item trước khi reject để lấy sellerId và title
                var item = await _itemService.GetByIdAsync(id);
                if (item == null)
                {
                    return NotFound(new { message = $"Item with ID {id} not found" });
                }

                var result = await _itemService.RejectItemAsync(id);
                if (!result)
                {
                    return NotFound(new { message = $"Item with ID {id} not found" });
                }

                // Tạo thông báo cho seller về việc sản phẩm bị từ chối
                try
                {
                    if (item.SellerId > 0)
                    {
                        // Truncate message nếu quá dài (max 500 chars)
                        var message = $"Sản phẩm '{item.Title}' của bạn đã bị từ chối";
                        if (message.Length > 500)
                        {
                            message = message.Substring(0, 497) + "...";
                        }

                        await _notificationService.CreateNotificationAsync(new CreateNotificationDto
                        {
                            UserId = item.SellerId,
                            Type = "item_rejected",
                            Message = message,
                            Link = $"/seller/items"
                        });

                        _logger.LogInformation("Created notification for seller {SellerId} about rejected item {ItemId} (Title: {Title})", item.SellerId, id, item.Title);
                    }
                    else
                    {
                        _logger.LogWarning("Cannot create notification for rejected item {ItemId}: SellerId is invalid ({SellerId})", id, item.SellerId);
                    }
                }
                catch (Exception ex)
                {
                    // Log error nhưng không fail request nếu notification creation fails
                    _logger.LogError(ex, "Failed to create notification for seller {SellerId} about rejected item {ItemId}: {Message}", item.SellerId, id, ex.Message);
                }

                // Gửi SignalR update để refresh admin dashboard real-time
                await NotifyPendingAndDashboardAsync(id, "rejected");

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

        private async Task NotifyPendingAndDashboardAsync(int itemId, string status)
        {
            var payload = new
            {
                itemId,
                status,
                timestamp = DateTime.UtcNow
            };

            await _auctionHub.Clients.Group(AuctionHub.AdminPendingGroup).SendAsync("AdminPendingItemsChanged", payload);
            await _auctionHub.Clients.Group(AuctionHub.AdminDashboardGroup).SendAsync("AdminStatsUpdated");
            await _auctionHub.Clients.Group(AuctionHub.AdminAnalyticsGroup).SendAsync("AdminAnalyticsUpdated");
        }
    }
}

