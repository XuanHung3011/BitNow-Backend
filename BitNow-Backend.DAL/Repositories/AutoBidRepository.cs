using BitNow_Backend.DAL.IRepositories;
using BitNow_Backend.DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace BitNow_Backend.DAL.Repositories
{
	public class AutoBidRepository : IAutoBidRepository
	{
		private readonly BidNowDbContext _ctx;

		public AutoBidRepository(BidNowDbContext ctx)
		{
			_ctx = ctx;
		}

		public async Task<AutoBid?> GetByAuctionAndUserAsync(int auctionId, int userId)
		{
			return await _ctx.AutoBids
				.FirstOrDefaultAsync(ab => ab.AuctionId == auctionId && ab.UserId == userId);
		}

		public async Task<AutoBid> CreateOrUpdateAsync(AutoBid autoBid)
		{
			var existing = await GetByAuctionAndUserAsync(autoBid.AuctionId, autoBid.UserId);
			if (existing != null)
			{
				existing.MaxAmount = autoBid.MaxAmount;
				existing.IsActive = autoBid.IsActive;
				existing.CreatedAt = autoBid.CreatedAt ?? DateTime.UtcNow;
				_ctx.AutoBids.Update(existing);
				await _ctx.SaveChangesAsync();
				return existing;
			}
			else
			{
				if (autoBid.CreatedAt == null)
					autoBid.CreatedAt = DateTime.UtcNow;
				_ctx.AutoBids.Add(autoBid);
				await _ctx.SaveChangesAsync();
				return autoBid;
			}
		}

		public async Task<IReadOnlyList<AutoBid>> GetActiveByAuctionAsync(int auctionId)
		{
			return await _ctx.AutoBids
				.Where(ab => ab.AuctionId == auctionId && ab.IsActive.HasValue && ab.IsActive.Value)
				.ToListAsync();
		}

		public async Task<bool> DeactivateAsync(int autoBidId)
		{
			var autoBid = await _ctx.AutoBids.FindAsync(autoBidId);
			if (autoBid == null) return false;
			autoBid.IsActive = false;
			await _ctx.SaveChangesAsync();
			return true;
		}
	}
}

