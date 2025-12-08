using BitNow_Backend.BLL.IServices;
using BitNow_Backend.DAL;
using BitNow_Backend.DAL.DTOs;
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
    /// Lấy tất cả khiếu nại (Admin only)
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<DisputeDto>>> GetAll()
    {
        try
        {
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
    /// Lấy khiếu nại theo status (Admin only)
    /// </summary>
    [HttpGet("status/{status}")]
    public async Task<ActionResult<IEnumerable<DisputeDto>>> GetByStatus(string status)
    {
        try
        {
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

            // Check authorization: buyer, seller, or admin can view
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized();

            var isAdmin = await IsAdminUserAsync(userId);
            if (dispute.BuyerId != userId && dispute.SellerId != userId && !isAdmin)
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

            // Check authorization
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized();

            var isAdmin = await IsAdminUserAsync(userId);
            if (dispute.BuyerId != userId && dispute.SellerId != userId && !isAdmin)
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

            var isAdmin = await IsAdminUserAsync(userId);
            if (userId != buyerId && !isAdmin)
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

            var isAdmin = await IsAdminUserAsync(userId);
            if (userId != sellerId && !isAdmin)
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

            // Notify all admins
            if (_notificationService != null)
            {
                try
                {
                    var adminUsers = await GetAdminUsersAsync();
                    foreach (var admin in adminUsers)
                    {
                        await _notificationService.CreateNotificationAsync(new CreateNotificationDto
                        {
                            UserId = admin.Id,
                            Message = $"Có khiếu nại mới từ đơn hàng #{dto.OrderId}: {dto.Reason}",
                            Type = "dispute_created",
                            Link = $"/admin?tab=disputes&disputeId={dispute.Id}"
                        });
                    }
                }
                catch (Exception notifEx)
                {
                    _logger.LogError(notifEx, "Failed to send notifications to admins for dispute {DisputeId}", dispute.Id);
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
    /// Bắt đầu xử lý khiếu nại (Admin only)
    /// </summary>
    [HttpPost("{id}/start-review")]
    public async Task<ActionResult<DisputeDto>> StartReview(int id)
    {
        try
        {
            var adminId = GetCurrentUserId();
            if (adminId == null)
                return Unauthorized();

            var dispute = await _disputeService.StartReviewAsync(id, adminId.Value);

            // Notify buyer and seller
            if (_notificationService != null)
            {
                try
                {
                    await _notificationService.CreateNotificationAsync(new CreateNotificationDto
                    {
                        UserId = dispute.BuyerId,
                        Message = $"Khiếu nại đơn hàng #{dispute.OrderId} đã được admin bắt đầu xử lý",
                        Type = "dispute_in_review",
                        Link = $"/messages?disputeId={dispute.Id}"
                    });

                    await _notificationService.CreateNotificationAsync(new CreateNotificationDto
                    {
                        UserId = dispute.SellerId,
                        Message = $"Khiếu nại đơn hàng #{dispute.OrderId} đã được admin bắt đầu xử lý",
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
    /// Giải quyết khiếu nại (Admin only)
    /// </summary>
    [HttpPost("{id}/resolve")]
    public async Task<ActionResult<DisputeDto>> Resolve(int id, [FromBody] ResolveDisputeDto dto)
    {
        try
        {
            var adminId = GetCurrentUserId();
            if (adminId == null)
                return Unauthorized();

            var dispute = await _disputeService.ResolveDisputeAsync(id, dto, adminId.Value);

            // Notify buyer and seller
            if (_notificationService != null)
            {
                try
                {
                    var winnerName = dto.Winner.ToLower() == "buyer" ? dispute.BuyerName : dispute.SellerName;
                    var message = dto.Winner.ToLower() == "buyer"
                        ? $"Khiếu nại đơn hàng #{dispute.OrderId} đã được giải quyết. Bạn thắng khiếu nại, tiền sẽ được hoàn lại."
                        : $"Khiếu nại đơn hàng #{dispute.OrderId} đã được giải quyết. {winnerName} thắng khiếu nại.";

                    await _notificationService.CreateNotificationAsync(new CreateNotificationDto
                    {
                        UserId = dispute.BuyerId,
                        Message = message,
                        Type = "dispute_resolved",
                        Link = $"/orders"
                    });

                    await _notificationService.CreateNotificationAsync(new CreateNotificationDto
                    {
                        UserId = dispute.SellerId,
                        Message = message,
                        Type = "dispute_resolved",
                        Link = $"/orders"
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
        if (userId == null) return false;
        
        var user = await _dbContext.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == userId);
        
        return user?.UserRoles.Any(ur => ur.Role.ToLower() == "admin") ?? false;
    }

    private async Task<List<DAL.Models.User>> GetAdminUsersAsync()
    {
        // Get all users with admin role
        return await _dbContext.Users
            .Include(u => u.UserRoles)
            .Where(u => u.UserRoles.Any(ur => ur.Role.ToLower() == "admin"))
            .ToListAsync();
    }
}

