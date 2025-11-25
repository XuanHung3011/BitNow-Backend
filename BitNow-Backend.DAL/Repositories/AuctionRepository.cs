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
                .FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task<(IEnumerable<Auction> auctions, int totalCount)> GetAuctionsWithFilterAsync(AuctionFilterDto filter)
        {
            var now = DateTime.UtcNow;
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

            // Filter by status
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
                    (normalizedStatuses.Contains("cancelled") && a.Status != null && a.Status.ToLower() == "cancelled")
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

                // Lưu thời gian tạm dừng khi status = "cancelled"
                if (string.Equals(status, "cancelled", StringComparison.OrdinalIgnoreCase))
                {
                    auction.PausedAt = DateTime.UtcNow;
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
                    var pausedDuration = DateTime.UtcNow - auction.PausedAt.Value;
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
            var now = DateTime.UtcNow;

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
            var now = DateTime.UtcNow;

            // Get auctions where user is the winner
            var query = _context.Auctions
                .Include(a => a.Item)
                    .ThenInclude(i => i.Category)
                .Include(a => a.Seller)
                .Include(a => a.Bids.Where(b => b.BidderId == bidderId))
                .Where(a => a.WinnerId == bidderId) // User is the winner
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
    }
}
