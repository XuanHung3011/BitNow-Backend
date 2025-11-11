using BitNow_Backend.DAL.IRepositories;
using BitNow_Backend.DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace BitNow_Backend.DAL.Repositories
{
	public class BidRepository : IBidRepository
	{
		private readonly BidNowDbContext _ctx;

		public BidRepository(BidNowDbContext ctx)
		{
			_ctx = ctx;
		}

		public async Task<Bid> AddAsync(Bid bid)
		{
			_ctx.Bids.Add(bid);
			await _ctx.SaveChangesAsync();
			return bid;
		}

		public async Task<IReadOnlyList<Bid>> GetRecentByAuctionAsync(int auctionId, int limit)
		{
			return await _ctx.Bids
				.Include(b => b.Bidder)
				.Where(b => b.AuctionId == auctionId)
				.OrderByDescending(b => b.BidTime)
				.Take(limit)
				.ToListAsync();
		}
	}
}


