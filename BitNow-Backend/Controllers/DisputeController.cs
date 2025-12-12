using BitNow_Backend.BLL.IServices;
using BitNow_Backend.DAL;
using BitNow_Backend.DAL.DTOs;
using BitNow_Backend.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BitNow_Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DisputeController : ControllerBase
{
    private readonly IDisputeService _disputeService;
    private readonly INotificationService? _notificationService;
    private readonly IMessageService _messageService;
    private readonly BidNowDbContext _dbContext;
    private readonly ILogger<DisputeController> _logger;

    public DisputeController(
        IDisputeService disputeService,
        INotificationService? notificationService,
        IMessageService messageService,
        BidNowDbContext dbContext,
        ILogger<DisputeController> logger)
    {
        _disputeService = disputeService;
        _notificationService = notificationService;
        _messageService = messageService;
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Lấy tất cả khiếu nại (Admin/Staff/Support only)
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<DisputeDto>>> GetAll()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized();

            // Chỉ admin hoặc staff được xem danh sách
            if (!await IsAdminOrStaffUserAsync(userId))
                return Forbid();

            var disputes = await _disputeService.GetAllAsync();
            return Ok(disputes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all disputes");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Lấy khiếu nại theo status (Admin/Staff/Support only)
    /// </summary>
    [HttpGet("status/{status}")]
    public async Task<ActionResult<IEnumerable<DisputeDto>>> GetByStatus(string status)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized();

            // Chỉ admin hoặc staff được xem danh sách
            if (!await IsAdminOrStaffUserAsync(userId))
                return Forbid();

            var disputes = await _disputeService.GetByStatusAsync(status);
            return Ok(disputes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting disputes by status {Status}", status);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Lấy khiếu nại theo ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<DisputeDto>> GetById(int id)
    {
        try
        {
            var dispute = await _disputeService.GetByIdAsync(id);
            if (dispute == null)
                return NotFound(new { message = "Dispute not found" });

            // Check authorization: buyer, seller, admin hoặc staff có thể xem
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized();

            var isAdminOrStaff = await IsAdminOrStaffUserAsync(userId);
            if (dispute.BuyerId != userId && dispute.SellerId != userId && !isAdminOrStaff)
                return Forbid();

            return Ok(dispute);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dispute {DisputeId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Lấy khiếu nại theo Order ID
    /// </summary>
    [HttpGet("order/{orderId}")]
    public async Task<ActionResult<DisputeDto>> GetByOrderId(int orderId)
    {
        try
        {
            var dispute = await _disputeService.GetByOrderIdAsync(orderId);
            if (dispute == null)
                return NotFound(new { message = "Dispute not found" });

            // Check authorization: buyer, seller, admin hoặc staff có thể xem
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized();

            var isAdminOrStaff = await IsAdminOrStaffUserAsync(userId);
            if (dispute.BuyerId != userId && dispute.SellerId != userId && !isAdminOrStaff)
                return Forbid();

            return Ok(dispute);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dispute for order {OrderId}", orderId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Lấy khiếu nại của buyer
    /// </summary>
    [HttpGet("buyer/{buyerId}")]
    public async Task<ActionResult<IEnumerable<DisputeDto>>> GetByBuyerId(int buyerId)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized();

            var isAdminOrStaff = await IsAdminOrStaffUserAsync(userId);
            if (userId != buyerId && !isAdminOrStaff)
                return Forbid();

            var disputes = await _disputeService.GetByBuyerIdAsync(buyerId);
            return Ok(disputes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting disputes for buyer {BuyerId}", buyerId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Lấy khiếu nại của seller
    /// </summary>
    [HttpGet("seller/{sellerId}")]
    public async Task<ActionResult<IEnumerable<DisputeDto>>> GetBySellerId(int sellerId)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized();

            var isAdminOrStaff = await IsAdminOrStaffUserAsync(userId);
            if (userId != sellerId && !isAdminOrStaff)
                return Forbid();

            var disputes = await _disputeService.GetBySellerIdAsync(sellerId);
            return Ok(disputes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting disputes for seller {SellerId}", sellerId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Tạo khiếu nại (Buyer only)
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<DisputeDto>> Create([FromBody] CreateDisputeDto dto)
    {
        try
        {
            var buyerId = GetCurrentUserId();
            if (buyerId == null)
                return Unauthorized();

            var dispute = await _disputeService.CreateDisputeAsync(dto, buyerId.Value);

            // Notify all admins, staff, and support users
            if (_notificationService != null)
            {
                try
                {
                    var adminStaffUsers = await GetAdminStaffUsersAsync();
                    foreach (var user in adminStaffUsers)
                    {
                        await _notificationService.CreateNotificationAsync(new CreateNotificationDto
                        {
                            UserId = user.Id,
                            Message = $"Có khiếu nại mới từ đơn hàng #{dto.OrderId}: {dto.Reason}",
                            Type = "dispute_created",
                            Link = $"/admin?tab=disputes&disputeId={dispute.Id}"
                        });
                    }
                }
                catch (Exception notifEx)
                {
                    _logger.LogError(notifEx, "Failed to send notifications to admins/staff/support for dispute {DisputeId}", dispute.Id);
                }
            }

            return Ok(dispute);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating dispute");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Bắt đầu xử lý khiếu nại (Admin/Staff only)
    /// </summary>
    [HttpPost("{id}/start-review")]
    public async Task<ActionResult<DisputeDto>> StartReview(int id)
    {
        try
        {
            var adminId = GetCurrentUserId();
            if (adminId == null)
                return Unauthorized();

            // Check if user is admin or staff
            if (!await IsAdminOrStaffUserAsync(adminId))
                return Forbid();

            var dispute = await _disputeService.StartReviewAsync(id, adminId.Value);

            // Notify buyer and seller
            if (_notificationService != null)
            {
                try
                {
                    await _notificationService.CreateNotificationAsync(new CreateNotificationDto
                    {
                        UserId = dispute.BuyerId,
                        Message = $"Khiếu nại đơn hàng #{dispute.OrderId} đã được admin/staff bắt đầu xử lý",
                        Type = "dispute_in_review",
                        Link = $"/messages?disputeId={dispute.Id}"
                    });

                    await _notificationService.CreateNotificationAsync(new CreateNotificationDto
                    {
                        UserId = dispute.SellerId,
                        Message = $"Khiếu nại đơn hàng #{dispute.OrderId} đã được admin/staff bắt đầu xử lý",
                        Type = "dispute_in_review",
                        Link = $"/messages?disputeId={dispute.Id}"
                    });
                }
                catch (Exception notifEx)
                {
                    _logger.LogError(notifEx, "Failed to send notifications for dispute {DisputeId}", id);
                }
            }

            return Ok(dispute);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting review for dispute {DisputeId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Giải quyết khiếu nại (Admin/Staff only)
    /// </summary>
    [HttpPost("{id}/resolve")]
    public async Task<ActionResult<DisputeDto>> Resolve(int id, [FromBody] ResolveDisputeDto dto)
    {
        try
        {
            var adminId = GetCurrentUserId();
            if (adminId == null)
                return Unauthorized();

            // Check if user is admin or staff
            if (!await IsAdminOrStaffUserAsync(adminId))
                return Forbid();

            var dispute = await _disputeService.ResolveDisputeAsync(id, dto, adminId.Value);

            // Notify buyer and seller with detailed resolution information
            if (_notificationService != null)
            {
                try
                {
                    var isBuyerWinner = dto.Winner.ToLower() == "buyer";
                    var buyerMessage = isBuyerWinner
                        ? $"Khiếu nại đơn hàng #{dispute.OrderId} đã được giải quyết. Bạn thắng khiếu nại, tiền sẽ được hoàn lại."
                        : $"Khiếu nại đơn hàng #{dispute.OrderId} đã được giải quyết. Người bán thắng khiếu nại.";
                    
                    var sellerMessage = !isBuyerWinner
                        ? $"Khiếu nại đơn hàng #{dispute.OrderId} đã được giải quyết. Bạn thắng khiếu nại, tiền sẽ được giải phóng."
                        : $"Khiếu nại đơn hàng #{dispute.OrderId} đã được giải quyết. Người mua thắng khiếu nại.";

                    // Add admin notes if available
                    if (!string.IsNullOrWhiteSpace(dto.AdminNotes))
                    {
                        buyerMessage += $"\n\nLý do: {dto.AdminNotes}";
                        sellerMessage += $"\n\nLý do: {dto.AdminNotes}";
                    }

                    await _notificationService.CreateNotificationAsync(new CreateNotificationDto
                    {
                        UserId = dispute.BuyerId,
                        Message = buyerMessage,
                        Type = "dispute_resolved",
                        Link = $"/buyer?tab=orders"
                    });

                    await _notificationService.CreateNotificationAsync(new CreateNotificationDto
                    {
                        UserId = dispute.SellerId,
                        Message = sellerMessage,
                        Type = "dispute_resolved",
                        Link = $"/seller?tab=orders"
                    });
                }
                catch (Exception notifEx)
                {
                    _logger.LogError(notifEx, "Failed to send notifications for resolved dispute {DisputeId}", id);
                }
            }

            return Ok(dispute);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving dispute {DisputeId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    private int? GetCurrentUserId()
    {
        // Try to get from header first (custom authentication)
        var userIdHeader = Request.Headers["X-User-Id"].FirstOrDefault();
        if (!string.IsNullOrEmpty(userIdHeader) && int.TryParse(userIdHeader, out var userId))
        {
            return userId;
        }

        // Fallback: try to get from User claims if available
        var userIdClaim = User.FindFirst("userId")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out userId))
            return null;
        return userId;
    }

    private async Task<bool> IsAdminUserAsync(int? userId)
    {
        return await RoleHelper.IsAdminAsync(_dbContext, userId);
    }

    private async Task<bool> IsAdminOrStaffUserAsync(int? userId)
    {
        return await RoleHelper.HasAnyRoleAsync(_dbContext, userId, "admin", "staff");
    }

    private async Task<List<DAL.Models.User>> GetAdminUsersAsync()
    {
        // Get all users with admin role
        return await _dbContext.Users
            .Include(u => u.UserRoles)
            .Where(u => u.UserRoles.Any(ur => ur.Role.ToLower() == "admin"))
            .ToListAsync();
    }

    private async Task<List<DAL.Models.User>> GetAdminStaffUsersAsync()
    {
        // Get all users with admin hoặc staff role
        return await _dbContext.Users
            .Include(u => u.UserRoles)
            .Where(u => u.UserRoles.Any(ur => 
                ur.Role.ToLower() == "admin" || 
                ur.Role.ToLower() == "staff"))
            .ToListAsync();
    }
}

