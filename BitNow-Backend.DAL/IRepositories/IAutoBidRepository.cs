using BitNow_Backend.DAL.Models;

namespace BitNow_Backend.DAL.IRepositories
{
	public interface IAutoBidRepository
	{
		Task<AutoBid?> GetByAuctionAndUserAsync(int auctionId, int userId);
		Task<AutoBid> CreateOrUpdateAsync(AutoBid autoBid);
		Task<IReadOnlyList<AutoBid>> GetActiveByAuctionAsync(int auctionId);
		Task<bool> DeactivateAsync(int autoBidId);
	}
}

