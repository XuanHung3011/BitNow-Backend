using BitNow_Backend.DAL.DTOs;
using BitNow_Backend.DAL.Models;

namespace BitNow_Backend.DAL.IRepositories
{
	public interface IAuctionRepository
	{
		Task<Auction?> GetByIdAsync(int id);
		Task<(IEnumerable<Auction> auctions, int totalCount)> GetAuctionsWithFilterAsync(AuctionFilterDto filter);

        Task<Auction> CreateAsync(Auction auction);
        Task<bool> UpdateStatusAsync(int id, string status);
        Task<bool> ResumeAuctionAsync(int id);

        Task<(IEnumerable<Auction> auctions, int totalCount)> GetAuctionsByBidderAsync(int bidderId, int page = 1, int pageSize = 10);

        Task<(IEnumerable<Auction> auctions, int totalCount)> GetWonAuctionsByBidderAsync(int bidderId, int page = 1, int pageSize = 10);
    }
}
