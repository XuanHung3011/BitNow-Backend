using BitNow_Backend.DAL.DTOs;

namespace BitNow_Backend.BLL.IServices
{
	public interface IBidService
	{
		Task<BidResultDto> PlaceBidAsync(int auctionId, int bidderId, decimal amount, bool isAutoBid = false);
		Task<IReadOnlyList<BidDto>> GetRecentBidsAsync(int auctionId, int limit);
		Task<decimal?> GetHighestBidAsync(int auctionId);
        Task<PaginatedResultB<BiddingHistoryDto>> GetBiddingHistoryAsync(int bidderId, int page, int pageSize);
        Task<IReadOnlyList<int>> GetDistinctBidderIdsByAuctionAsync(int auctionId);
    }
}



