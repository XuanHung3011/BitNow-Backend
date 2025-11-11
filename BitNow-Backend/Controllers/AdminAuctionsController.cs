using BitNow_Backend.BLL.IServices;
using BitNow_Backend.DAL.DTOs;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BitNow_Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdminAuctionsController : ControllerBase
{
    private readonly IAuctionService _auctionService;
    private readonly ILogger<AdminAuctionsController> _logger;

    public AdminAuctionsController(IAuctionService auctionService, ILogger<AdminAuctionsController> logger)
    {
        _auctionService = auctionService;
        _logger = logger;
    }

    /// <summary>
    /// Get all auctions with pagination, search, and status filtering
    /// </summary>
    /// <param name="searchTerm">Search by item title or seller name</param>
    /// <param name="statuses">Filter by status: 'active', 'scheduled', 'completed', 'suspended' (comma-separated for multiple)</param>
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
                var validStatuses = new[] { "active", "scheduled", "completed", "suspended" };
                var invalidStatuses = statusList.Where(s => !validStatuses.Contains(s, StringComparer.OrdinalIgnoreCase)).ToList();
                if (invalidStatuses.Any())
                {
                    return BadRequest(new { message = $"Invalid status values: {string.Join(", ", invalidStatuses)}. Valid values are: {string.Join(", ", validStatuses)}" });
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
}

