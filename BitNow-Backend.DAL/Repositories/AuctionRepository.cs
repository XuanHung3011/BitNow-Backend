using BitNow_Backend.DAL.DTOs;
using BitNow_Backend.DAL.IRepositories;
using BitNow_Backend.DAL.Models;
using Microsoft.EntityFrameworkCore;
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
					(normalizedStatuses.Contains("suspended") && a.Status != null && a.Status.ToLower() == "suspended")
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
	}
}
