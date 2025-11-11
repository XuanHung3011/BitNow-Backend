using BitNow_Backend.DAL.DTOs;
using BitNow_Backend.DAL.Models;

namespace BitNow_Backend.DAL.IRepositories
{
	public interface IAuctionRepository
	{
		Task<Auction?> GetByIdAsync(int id);
		Task<(IEnumerable<Auction> auctions, int totalCount)> GetAuctionsWithFilterAsync(AuctionFilterDto filter);
	}
}
