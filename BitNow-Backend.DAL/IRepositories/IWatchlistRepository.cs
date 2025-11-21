using BitNow_Backend.DAL.Models;

namespace BitNow_Backend.DAL.IRepositories
{
	public interface IWatchlistRepository
	{
		Task<Watchlist?> GetAsync(int userId, int auctionId);
		Task<Watchlist?> GetByIdAsync(int id);
		Task<IEnumerable<Watchlist>> GetByUserAsync(int userId);
		Task<Watchlist> AddAsync(Watchlist entity);
		Task<bool> RemoveAsync(int userId, int auctionId);
		Task<bool> ExistsAsync(int userId, int auctionId);
		Task<IReadOnlyList<int>> GetDistinctUserIdsByAuctionAsync(int auctionId);
	}
}
