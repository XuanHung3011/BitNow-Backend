using BitNow_Backend.DAL.DTOs;

namespace BitNow_Backend.BLL.IServices
{
	public interface IWatchlistService
	{
		Task<bool> AddAsync(AddToWatchlistRequest request);
		Task<bool> RemoveAsync(RemoveFromWatchlistRequest request);
		Task<IEnumerable<WatchlistItemDto>> GetByUserAsync(int userId);
		Task<WatchlistItemDto?> GetDetailAsync(int watchlistId);
		Task<WatchlistItemDto?> GetDetailByUserAuctionAsync(int userId, int auctionId);
		Task<IReadOnlyList<int>> GetDistinctUserIdsByAuctionAsync(int auctionId);
	}
}
