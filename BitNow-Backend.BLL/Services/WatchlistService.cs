using BitNow_Backend.BLL.IServices;
using BitNow_Backend.DAL.DTOs;
using BitNow_Backend.DAL.IRepositories;
using BitNow_Backend.DAL.Models;

namespace BitNow_Backend.BLL.Services
{
	public class WatchlistService : IWatchlistService
	{
		private readonly IWatchlistRepository _watchlistRepository;

		public WatchlistService(IWatchlistRepository watchlistRepository)
		{
			_watchlistRepository = watchlistRepository;
		}

		public async Task<bool> AddAsync(AddToWatchlistRequest request)
		{
			if (await _watchlistRepository.ExistsAsync(request.UserId, request.AuctionId))
			{
				return true; // idempotent add
			}

			var entity = new Watchlist
			{
				UserId = request.UserId,
				AuctionId = request.AuctionId,
				AddedAt = DateTime.UtcNow
			};

			await _watchlistRepository.AddAsync(entity);
			return true;
		}

		public async Task<bool> RemoveAsync(RemoveFromWatchlistRequest request)
		{
			return await _watchlistRepository.RemoveAsync(request.UserId, request.AuctionId);
		}

		public async Task<IEnumerable<WatchlistItemDto>> GetByUserAsync(int userId)
		{
			var items = await _watchlistRepository.GetByUserAsync(userId);
			return items.Select(w => Map(w));
		}

		public async Task<WatchlistItemDto?> GetDetailAsync(int watchlistId)
		{
			var w = await _watchlistRepository.GetByIdAsync(watchlistId);
			return w == null ? null : Map(w);
		}

		public async Task<WatchlistItemDto?> GetDetailByUserAuctionAsync(int userId, int auctionId)
		{
			var w = await _watchlistRepository.GetAsync(userId, auctionId);
			if (w == null)
			{
				return null;
			}
			// ensure auction and item are loaded
			w = await _watchlistRepository.GetByIdAsync(w.Id);
			return w == null ? null : Map(w);
		}

		private static WatchlistItemDto Map(Watchlist w)
		{
			return new WatchlistItemDto
			{
				WatchlistId = w.Id,
				UserId = w.UserId,
				AuctionId = w.AuctionId,
				AddedAt = w.AddedAt,
				ItemTitle = w.Auction.Item.Title,
				StartingBid = w.Auction.StartingBid,
				CurrentBid = w.Auction.CurrentBid,
				BuyNowPrice = w.Auction.BuyNowPrice,
				EndTime = w.Auction.EndTime,
				Status = w.Auction.Status
			};
		}
	}
}
