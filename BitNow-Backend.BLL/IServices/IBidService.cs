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
		/// <summary>
		/// Đặt TTL (Time To Live) cho các Redis keys của auction sau khi auction kết thúc.
		/// Sau khi TTL hết hạn, Redis sẽ tự động xóa các keys này để giải phóng bộ nhớ.
		/// </summary>
		/// <param name="auctionId">ID của auction đã kết thúc</param>
		/// <param name="expirationMinutes">Số phút trước khi keys bị xóa (mặc định: 30 phút)</param>
		Task SetAuctionCacheExpirationAsync(int auctionId, int expirationMinutes = 30);
    }
}



