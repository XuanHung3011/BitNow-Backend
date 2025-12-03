using BitNow_Backend.DAL.DTOs;
using System.Threading;

namespace BitNow_Backend.BLL.IServices
{
	public interface IAuctionService
	{
		Task<AuctionDetailDto?> GetDetailAsync(int id);
		Task<PaginatedResult<AuctionListItemDto>> GetAuctionsWithFilterAsync(AuctionFilterDto filter);
        Task<bool> UpdateStatusAsync(int id, string status);
        Task<bool> ResumeAuctionAsync(int id);

        Task<AuctionResponseDto?> CreateAuctionAsync(CreateAuctionDto dto);

        Task<PaginatedResult<BuyerActiveBidDto>> GetActiveBidsByBuyerAsync(int bidderId, int page = 1, int pageSize = 10);
        Task<PaginatedResult<BuyerWonAuctionDto>> GetWonAuctionsByBuyerAsync(int bidderId, int page = 1, int pageSize = 10);

        Task<List<SellerAuctionDto>> GetAuctionsBySellerAsync(int sellerId);


        Task<IEnumerable<ItemResponseDto>> GetItemsByAuctionIdsAsync(IEnumerable<int> auctionIds);

        Task<AuctionCompletionResultDto> BuyNowAsync(int auctionId, int buyerId);
        Task<IReadOnlyList<AuctionCompletionResultDto>> FinalizeExpiredAuctionsAsync(CancellationToken cancellationToken = default);

    }
}
