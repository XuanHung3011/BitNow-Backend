using BitNow_Backend.BLL.IServices;
using BitNow_Backend.DAL.DTOs;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.SignalR;
using BitNow_Backend.RealTime;

namespace BitNow_Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdminAuctionsController : ControllerBase
{
    private readonly IAuctionService _auctionService;
    private readonly ILogger<AdminAuctionsController> _logger;
    private readonly IHubContext<AuctionHub> _auctionHub;
    private readonly INotificationService _notificationService;
    private static readonly string[] ValidFilterStatuses = { "active", "scheduled", "completed", "cancelled" };
    private static readonly string[] AllowedStatusUpdates = { "draft", "active", "completed", "cancelled" };

    public AdminAuctionsController(
        IAuctionService auctionService,
        ILogger<AdminAuctionsController> logger,
        IHubContext<AuctionHub> auctionHub,
        INotificationService notificationService)
    {
        _auctionService = auctionService;
        _logger = logger;
        _auctionHub = auctionHub;
        _notificationService = notificationService;
    }

    /// <summary>
    /// Get all auctions with pagination, search, and status filtering
    /// </summary>
    /// <param name="searchTerm">Search by item title or seller name</param>
    /// <param name="statuses">Filter by status: 'active', 'scheduled', 'completed', 'cancelled' (comma-separated for multiple)</param>
    /// <param name="sortBy">Sort by: 'ItemTitle', 'EndTime', 'CurrentBid', 'BidCount' (default: 'EndTime')</param>
    /// <param name="sortOrder">Sort order: 'asc' or 'desc' (default: 'desc')</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 10, max: 100)</param>
    [HttpGet]
    public async Task<ActionResult<PaginatedResult<AuctionListItemDto>>> GetAuctions(
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? statuses = null,
        [FromQuery] string? sortBy = "EndTime",
        [FromQuery] string? sortOrder = "desc",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            // Validate parameters
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 10;
            if (string.IsNullOrEmpty(sortBy)) sortBy = "EndTime";
            if (string.IsNullOrEmpty(sortOrder)) sortOrder = "desc";

            // Validate sortBy values
            var validSortBy = new[] { "ItemTitle", "EndTime", "CurrentBid", "BidCount" };
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
                var invalidStatuses = statusList.Where(s => !ValidFilterStatuses.Contains(s, StringComparer.OrdinalIgnoreCase)).ToList();
                if (invalidStatuses.Any())
                {
                    return BadRequest(new { message = $"Invalid status values: {string.Join(", ", invalidStatuses)}. Valid values are: {string.Join(", ", ValidFilterStatuses)}" });
                }
            }

            var filter = new AuctionFilterDto
            {
                SearchTerm = searchTerm,
                Statuses = statusList,
                SortBy = sortBy,
                SortOrder = sortOrder,
                Page = page,
                PageSize = pageSize
            };

            var result = await _auctionService.GetAuctionsWithFilterAsync(filter);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting auctions");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get auction detail by ID
    /// </summary>
    /// <param name="id">Auction ID</param>
    [HttpGet("{id}")]
    public async Task<ActionResult<AuctionDetailDto>> GetAuctionDetail(int id)
    {
        try
        {
            var auction = await _auctionService.GetDetailAsync(id);
            if (auction == null)
            {
                return NotFound(new { message = $"Auction with ID {id} not found" });
            }

            return Ok(auction);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting auction detail {AuctionId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Update auction status (draft, active, completed, cancelled)
    /// </summary>
    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateAuctionStatusRequest request)
    {
        try
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Status))
            {
                return BadRequest(new { message = "Status is required" });
            }

            var normalizedStatus = request.Status.Trim().ToLower();
            if (!AllowedStatusUpdates.Contains(normalizedStatus, StringComparer.OrdinalIgnoreCase))
            {
                return BadRequest(new { message = $"Status must be one of: {string.Join(", ", AllowedStatusUpdates)}" });
            }

            var auction = await _auctionService.GetDetailAsync(id);
            if (auction == null)
            {
                return NotFound(new { message = $"Auction with ID {id} not found" });
            }

            if (string.Equals(normalizedStatus, "cancelled", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Trim().Length < 10)
                {
                    return BadRequest(new { message = "Nguyên nhân tạm dừng phải có ít nhất 10 ký tự." });
                }

                if (!string.Equals(request.AdminSignature?.Trim(), "Admin", StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest(new { message = "Bạn phải nhập chính xác chữ ký 'Admin' để xác nhận." });
                }
            }

            var updated = await _auctionService.UpdateStatusAsync(id, normalizedStatus);
            if (!updated)
            {
                return NotFound(new { message = $"Auction with ID {id} not found" });
            }

            var payload = new
            {
                auctionId = id,
                status = normalizedStatus,
                timestamp = DateTime.UtcNow
            };
            await _auctionHub.Clients.Group(AuctionHub.AdminAuctionsGroup).SendAsync("AdminAuctionStatusUpdated", payload);
            await _auctionHub.Clients.Group(AuctionHub.AdminDashboardGroup).SendAsync("AdminStatsUpdated");
            await _auctionHub.Clients.Group(AuctionHub.AdminAnalyticsGroup).SendAsync("AdminAnalyticsUpdated");

            if (string.Equals(normalizedStatus, "cancelled", StringComparison.OrdinalIgnoreCase))
            {
                var message = $"Phiên đấu giá \"{auction.ItemTitle}\" đã bị tạm dừng bởi Admin.\nLý do: {request.Reason?.Trim()}\nNgười phê duyệt: {request.AdminSignature?.Trim() ?? "Admin"}";
                try
                {
                    await _notificationService.CreateNotificationAsync(new CreateNotificationDto
                    {
                        UserId = auction.SellerId,
                        Type = "auction-suspended",
                        Message = message,
                        Link = $"/seller/auctions/{auction.Id}"
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create suspension notification for auction {AuctionId}", auction.Id);
                }
            }

            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating auction status {AuctionId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    public class UpdateAuctionStatusRequest
    {
        public string Status { get; set; } = string.Empty;
        public string? Reason { get; set; }
        public string? AdminSignature { get; set; }
    }
}

