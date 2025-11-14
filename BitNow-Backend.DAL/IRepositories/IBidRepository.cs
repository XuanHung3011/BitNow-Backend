using BitNow_Backend.DAL.Models;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace BitNow_Backend.DAL.IRepositories
{
	public interface IBidRepository
	{
		Task<Bid> AddAsync(Bid bid);
		Task<IReadOnlyList<Bid>> GetRecentByAuctionAsync(int auctionId, int limit);
        Task<IReadOnlyList<Bid>> GetBidsByBidderAsync(int bidderId, int skip, int take);
        Task<int> GetTotalBidCountByBidderAsync(int bidderId);
    }
}


