using BitNow_Backend.DAL.Models;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace BitNow_Backend.DAL.IRepositories
{
	public interface IBidRepository
	{
		Task<Bid> AddAsync(Bid bid);
		Task<IReadOnlyList<Bid>> GetRecentByAuctionAsync(int auctionId, int limit);
	}
}


