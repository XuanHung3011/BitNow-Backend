using BitNow_Backend.DAL.IRepositories;
using BitNow_Backend.DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace BitNow_Backend.DAL.Repositories
{
	public class WatchlistRepository : IWatchlistRepository
	{
		private readonly BidNowDbContext _context;

		public WatchlistRepository(BidNowDbContext context)
		{
			_context = context;
		}

		public async Task<Watchlist?> GetAsync(int userId, int auctionId)
		{
			return await _context.Watchlists
                .Include(w => w.Auction)
                    .ThenInclude(a => a.Item)
                        .ThenInclude(i => i.Category)
                .FirstOrDefaultAsync(w => w.UserId == userId && w.AuctionId == auctionId);
		}

		public async Task<Watchlist?> GetByIdAsync(int id)
		{
			return await _context.Watchlists.Include(w => w.Auction).ThenInclude(a => a.Item).ThenInclude(i => i.Category).FirstOrDefaultAsync(w => w.Id == id);
		}

		public async Task<IEnumerable<Watchlist>> GetByUserAsync(int userId)
		{
			return await _context.Watchlists
				.Include(w => w.Auction)
				.ThenInclude(a => a.Item)
                .ThenInclude(i => i.Category)
                .Where(w => w.UserId == userId)
				.OrderByDescending(w => w.AddedAt)
				.ToListAsync();
		}

		public async Task<Watchlist> AddAsync(Watchlist entity)
		{
			_context.Watchlists.Add(entity);
			await _context.SaveChangesAsync();
			return entity;
		}

		public async Task<bool> RemoveAsync(int userId, int auctionId)
		{
			var entity = await _context.Watchlists.FirstOrDefaultAsync(w => w.UserId == userId && w.AuctionId == auctionId);
			if (entity == null) return false;
			_context.Watchlists.Remove(entity);
			await _context.SaveChangesAsync();
			return true;
		}

		public async Task<bool> ExistsAsync(int userId, int auctionId)
		{
			return await _context.Watchlists.AnyAsync(w => w.UserId == userId && w.AuctionId == auctionId);
		}

		public async Task<IReadOnlyList<int>> GetDistinctUserIdsByAuctionAsync(int auctionId)
		{
			return await _context.Watchlists
				.Where(w => w.AuctionId == auctionId)
				.Select(w => w.UserId)
				.Distinct()
				.ToListAsync();
		}
	}
}
