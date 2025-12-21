using BitNow_Backend.DAL.DTOs;
using BitNow_Backend.DAL.IRepositories;
using BitNow_Backend.DAL.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace BitNow_Backend.DAL.Repositories
{
    public class AuctionRepository : IAuctionRepository
    {
        private readonly BidNowDbContext _context;

        public AuctionRepository(BidNowDbContext context)
        {
            _context = context;
        }

        public async Task<Auction?> GetByIdAsync(int id)
        {
            return await _context.Auctions
                .Include(a => a.Item)
                    .ThenInclude(i => i.Category)
                .Include(a => a.Seller)
                .Include(a => a.Winner)
                .FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task<(IEnumerable<Auction> auctions, int totalCount)> GetAuctionsWithFilterAsync(AuctionFilterDto filter)
        {
            var now = DateTime.Now;
            var query = _context.Auctions
                .Include(a => a.Item)
                    .ThenInclude(i => i.Category)
                .Include(a => a.Seller)
                .AsQueryable();

            // Search by item title or seller name
            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var term = filter.SearchTerm.ToLower().Trim();
                query = query.Where(a =>
                    (a.Item != null && EF.Functions.Like(a.Item.Title.ToLower(), $"%{term}%")) ||
                    (a.Seller != null && EF.Functions.Like(a.Seller.FullName.ToLower(), $"%{term}%"))
                );
            }

            // Filter by category
            if (filter.CategoryId.HasValue && filter.CategoryId.Value > 0)
            {
                query = query.Where(a => a.Item != null && a.Item.CategoryId == filter.CategoryId.Value);
            }

            // Only show auctions with approved items (or archived items that have active auctions)
            query = query.Where(a => a.Item != null && 
                (a.Item.Status == "approved" || 
                 (a.Item.Status == "archived" && a.EndTime > now)));

            // Filter by status - filter based on actual time, not just Status field
            if (filter.Statuses != null && filter.Statuses.Any())
            {
                var normalizedStatuses = filter.Statuses.Select(s => s.ToLower()).ToList();

                query = query.Where(a =>
                    (normalizedStatuses.Contains("active") && a.Status != null && a.Status.ToLower() == "active" &&
                        a.StartTime <= now && a.EndTime > now) ||
                    (normalizedStatuses.Contains("scheduled") && a.Status != null && a.Status.ToLower() == "active" &&
                        a.StartTime > now) ||
                    (normalizedStatuses.Contains("completed") && (a.EndTime < now ||
                        (a.Status != null && a.Status.ToLower() == "completed"))) ||
                    (normalizedStatuses.Contains("paused") && a.Status != null && a.Status.ToLower() == "paused") ||
                    (normalizedStatuses.Contains("cancelled") && a.Status != null && a.Status.ToLower() == "cancelled")
                );
            }
            else
            {
                // By default, exclude completed and cancelled auctions unless explicitly requested
                // Only show active, scheduled, and paused auctions
                query = query.Where(a =>
                    a.Status != null &&
                    a.Status.ToLower() != "completed" &&
                    a.Status.ToLower() != "cancelled" &&
                    a.Status.ToLower() != "canceled" &&
                    // Also exclude ended auctions based on time
                    a.EndTime > now
                );
            }

            // Get total count before pagination
            var totalCount = await query.CountAsync();

            // Sorting
            var sortBy = filter.SortBy?.ToLower() ?? "endtime";
            var sortOrder = filter.SortOrder?.ToLower() ?? "desc";

            switch (sortBy)
            {
                case "itemtitle":
                    query = sortOrder == "asc"
                        ? query.OrderBy(a => a.Item != null ? a.Item.Title : "")
                        : query.OrderByDescending(a => a.Item != null ? a.Item.Title : "");
                    break;
                case "currentbid":
                    query = sortOrder == "asc"
                        ? query.OrderBy(a => a.CurrentBid ?? a.StartingBid)
                        : query.OrderByDescending(a => a.CurrentBid ?? a.StartingBid);
                    break;
                case "bidcount":
                    query = sortOrder == "asc"
                        ? query.OrderBy(a => a.BidCount ?? 0)
                        : query.OrderByDescending(a => a.BidCount ?? 0);
                    break;
                case "endtime":
                default:
                    query = sortOrder == "asc"
                        ? query.OrderBy(a => a.EndTime)
                        : query.OrderByDescending(a => a.EndTime);
                    break;
            }

            // Pagination
            var page = filter.Page < 1 ? 1 : filter.Page;
            var pageSize = filter.PageSize < 1 ? 10 : (filter.PageSize > 100 ? 100 : filter.PageSize);
            var skip = (page - 1) * pageSize;

            var auctions = await query
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync();

            return (auctions, totalCount);
        }

        public async Task<Auction> CreateAsync(Auction auction)
        {
            try
            {
                // Create a new entity with only foreign key IDs
                // Don't set navigation properties to avoid relationship issues
                var newAuction = new Auction
                {
                    ItemId = auction.ItemId,
                    SellerId = auction.SellerId,
                    StartingBid = auction.StartingBid,
                    BuyNowPrice = auction.BuyNowPrice,
                    StartTime = auction.StartTime,
                    EndTime = auction.EndTime,
                    Status = auction.Status,
                    BidCount = auction.BidCount,
                    CurrentBid = auction.CurrentBid,
                    CreatedAt = auction.CreatedAt,
                    WinnerId = auction.WinnerId
                };

                // Add the new entity (without navigation properties)
                _context.Auctions.Add(newAuction);
                await _context.SaveChangesAsync();

                // Reload with includes to get full data
                return await GetByIdAsync(newAuction.Id) ?? newAuction;
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
            {
                // Log inner exception for debugging
                var innerMessage = ex.InnerException?.Message ?? ex.Message;
                throw new Exception($"Database error: {innerMessage}", ex);
            }
        }
        public async Task<bool> UpdateStatusAsync(int id, string status)
        {
            try
            {
                var auction = await _context.Auctions.FirstOrDefaultAsync(a => a.Id == id);
                if (auction == null)
                {
                    return false;
                }

                auction.Status = status;

                // Lưu thời gian tạm dừng khi status = "paused"
                if (string.Equals(status, "paused", StringComparison.OrdinalIgnoreCase))
                {
                    auction.PausedAt = DateTime.Now;
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException ex)
            {
                var innerMessage = ex.InnerException?.Message ?? ex.Message;
                throw new Exception($"Database error: {innerMessage}", ex);
            }
        }

        public async Task<bool> ResumeAuctionAsync(int id)
        {
            try
            {
                var auction = await _context.Auctions.FirstOrDefaultAsync(a => a.Id == id);
                if (auction == null)
                {
                    return false;
                }

                // Kiểm tra nếu có thời gian tạm dừng, tính và cộng vào EndTime
                if (auction.PausedAt.HasValue)
                {
                    var pausedDuration = DateTime.Now - auction.PausedAt.Value;
                    auction.EndTime = auction.EndTime.Add(pausedDuration);
                }

                // Cập nhật status thành active
                auction.Status = "active";
                // Xóa PausedAt vì auction đã được tiếp tục, không còn tạm dừng nữa
                auction.PausedAt = null;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException ex)
            {
                var innerMessage = ex.InnerException?.Message ?? ex.Message;
                throw new Exception($"Database error: {innerMessage}", ex);
            }
        }
        public async Task<(IEnumerable<Auction> auctions, int totalCount)> GetAuctionsByBidderAsync(int bidderId, int page = 1, int pageSize = 10)
        {
            var now = DateTime.Now;

            // Get distinct auction IDs where user has placed bids
            var auctionIdsQuery = _context.Bids
                .Where(b => b.BidderId == bidderId)
                .Select(b => b.AuctionId)
                .Distinct();

            var query = _context.Auctions
                .Include(a => a.Item)
                    .ThenInclude(i => i.Category)
                .Include(a => a.Seller)
                .Include(a => a.Bids.Where(b => b.BidderId == bidderId)) // Include user's bids
                .Where(a => auctionIdsQuery.Contains(a.Id))
                .Where(a => a.Status == "active" && a.EndTime > now) // Only active auctions
                .OrderByDescending(a => a.EndTime);

            var totalCount = await query.CountAsync();

            var skip = (page - 1) * pageSize;
            var auctions = await query
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync();

            return (auctions, totalCount);
        }
        public async Task<(IEnumerable<Auction> auctions, int totalCount)> GetWonAuctionsByBidderAsync(int bidderId, int page = 1, int pageSize = 10)
        {
            var now = DateTime.Now;

            // CRITICAL: Get auctions where user is the winner
            // Có 2 cách để xác định winner (theo thứ tự ưu tiên):
            // 1. WinnerId trong Auction == bidderId (đã được set khi finalize) - ƯU TIÊN CAO NHẤT
            // 2. WinnerId trong AuctionHistory == bidderId (fallback nếu WinnerId trong Auction chưa được set)
            
            // Lấy auction IDs từ AuctionHistory (nếu WinnerId trong Auction chưa được set)
            var historyWinnerIds = await _context.AuctionHistories
                .Where(h => h.WinnerId == bidderId)
                .Select(h => h.AuctionId)
                .ToListAsync();

            var query = _context.Auctions
                .Include(a => a.Item)
                    .ThenInclude(i => i.Category)
                .Include(a => a.Seller)
                .Include(a => a.Bids.Where(b => b.BidderId == bidderId))
                .Where(a => 
                    // Cách 1: WinnerId trong Auction đã được set và match với bidderId
                    (a.WinnerId.HasValue && a.WinnerId.Value == bidderId) ||
                    // Cách 2: WinnerId trong AuctionHistory match với bidderId (fallback)
                    historyWinnerIds.Contains(a.Id)
                )
                .Where(a => a.Status == "completed" || a.EndTime < now) // Auction has ended
                .OrderByDescending(a => a.EndTime); // Most recent first

            var totalCount = await query.CountAsync();

            var skip = (page - 1) * pageSize;
            var auctions = await query
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync();

            return (auctions, totalCount);
        }

        public async Task<IEnumerable<Auction>> GetAuctionsBySellerAsync(int sellerId)
        {
            return await _context.Auctions
                .Include(a => a.Item)
                    .ThenInclude(i => i.Category)
                .Include(a => a.Winner)
                .Where(a => a.SellerId == sellerId)
                .OrderByDescending(a => a.StartTime)
                .ToListAsync();
        }


        public async Task<IEnumerable<Auction>> GetAuctionsByIdsAsync(IEnumerable<int> auctionIds)
        {
            if (auctionIds == null || !auctionIds.Any())
            {
                return Enumerable.Empty<Auction>();
            }

            var now = DateTime.UtcNow;

            var allowedStatuses = new[] { "active", "scheduled" };

            return await _context.Auctions
                .Include(a => a.Item)
                    .ThenInclude(i => i.Category)
                .Include(a => a.Seller)
                .Where(a =>
                    auctionIds.Contains(a.Id) &&
                    a.Status != null && allowedStatuses.Contains(a.Status.ToLower()) &&
                    (a.Item.Status == "approved" || a.Item.Status == "archived") &&
                    a.EndTime > now
                )
                .ToListAsync();
        }

        public async Task<int> UpdateScheduledToActiveAsync()
        {
            var now = DateTime.Now; // Use local time (Vietnam time)
            // Find auctions that should be active:
            // Status is "scheduled" and StartTime has passed (but EndTime hasn't)
            var scheduledAuctions = await _context.Auctions
                .Where(a => a.Status != null && 
                           a.Status.ToLower() == "scheduled" &&
                           a.StartTime <= now &&
                           a.EndTime > now)
                .ToListAsync();

            foreach (var auction in scheduledAuctions)
            {
                auction.Status = "active";
            }

            if (scheduledAuctions.Any())
            {
                await _context.SaveChangesAsync();
            }

            return scheduledAuctions.Count;
        }

        public async Task<int> UpdateActiveToCompletedAsync()
        {
            var now = DateTime.Now; // Use local time (Vietnam time)
            var activeAuctions = await _context.Auctions
                .Where(a => a.Status != null && 
                           a.Status.ToLower() == "active" &&
                           a.EndTime <= now)
                .ToListAsync();

            foreach (var auction in activeAuctions)
            {
                auction.Status = "completed";
            }

            if (activeAuctions.Any())
            {
                await _context.SaveChangesAsync();
            }

            return activeAuctions.Count;
        }

        public async Task<bool> UpdateAuctionStatusIfNeededAsync(int auctionId)
        {
            var now = DateTime.Now; // Use local time (Vietnam time)
            var auction = await _context.Auctions.FirstOrDefaultAsync(a => a.Id == auctionId);
            
            if (auction == null || auction.Status == null)
            {
                return false;
            }

            var statusLower = auction.Status.ToLower();
            bool updated = false;

            // Update scheduled to active if StartTime has passed
            if (statusLower == "scheduled" && auction.StartTime <= now && auction.EndTime > now)
            {
                auction.Status = "active";
                updated = true;
            }
            // Update active to completed if EndTime has passed
            else if (statusLower == "active" && auction.EndTime <= now)
            {
                auction.Status = "completed";
                updated = true;
            }

            if (updated)
            {
                await _context.SaveChangesAsync();
            }

            return updated;

        }
    }
}
