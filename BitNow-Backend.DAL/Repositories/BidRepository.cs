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
			// Include(b => b.Bidder) để khi Redis cache trống, service vẫn có tên full của người đặt giá.
			return await _ctx.Bids
				.Include(b => b.Bidder)
				.Where(b => b.AuctionId == auctionId)
				.OrderByDescending(b => b.BidTime)
				.Take(limit)
				.ToListAsync();
		}
        public async Task<IReadOnlyList<Bid>> GetBidsByBidderAsync(int bidderId, int skip, int take)
        {
            return await _ctx.Bids
                .Include(b => b.Auction)
                    .ThenInclude(a => a.Item)
                        .ThenInclude(i => i.Category)
                .Where(b => b.BidderId == bidderId)
                .OrderByDescending(b => b.BidTime)
                .Skip(skip)
                .Take(take)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<int> GetTotalBidCountByBidderAsync(int bidderId)
        {
            return await _ctx.Bids
                .Where(b => b.BidderId == bidderId)
                .CountAsync();
        }

        public async Task<IReadOnlyList<int>> GetDistinctBidderIdsByAuctionAsync(int auctionId)
        {
            return await _ctx.Bids
                .Where(b => b.AuctionId == auctionId)
                .Select(b => b.BidderId)
                .Distinct()
                .ToListAsync();
        }
    }
}


