using BitNow_Backend.DAL.DTOs;

namespace BitNow_Backend.BLL.IServices
{
	public interface IAuctionService
	{
		Task<AuctionDetailDto?> GetDetailAsync(int id);
		Task<PaginatedResult<AuctionListItemDto>> GetAuctionsWithFilterAsync(AuctionFilterDto filter);

        Task<AuctionResponseDto?> CreateAuctionAsync(CreateAuctionDto dto);

        Task<PaginatedResult<BuyerActiveBidDto>> GetActiveBidsByBuyerAsync(int bidderId, int page = 1, int pageSize = 10);
    }
}
