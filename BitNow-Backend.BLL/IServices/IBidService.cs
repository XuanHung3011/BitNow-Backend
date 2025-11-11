using BitNow_Backend.DAL.DTOs;

namespace BitNow_Backend.BLL.IServices
{
	public interface IBidService
	{
		Task<BidResultDto> PlaceBidAsync(int auctionId, int bidderId, decimal amount);
		Task<IReadOnlyList<BidDto>> GetRecentBidsAsync(int auctionId, int limit);
		Task<decimal?> GetHighestBidAsync(int auctionId);
	}
}


