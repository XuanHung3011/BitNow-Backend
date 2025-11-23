using BitNow_Backend.DAL.DTOs;

namespace BitNow_Backend.BLL.IServices
{
	public interface IAuctionService
	{
		Task<AuctionDetailDto?> GetDetailAsync(int id);
		Task<PaginatedResult<AuctionListItemDto>> GetAuctionsWithFilterAsync(AuctionFilterDto filter);
        Task<bool> UpdateStatusAsync(int id, string status);

        Task<AuctionResponseDto?> CreateAuctionAsync(CreateAuctionDto dto);

        Task<PaginatedResult<BuyerActiveBidDto>> GetActiveBidsByBuyerAsync(int bidderId, int page = 1, int pageSize = 10);
        Task<PaginatedResult<BuyerWonAuctionDto>> GetWonAuctionsByBuyerAsync(int bidderId, int page = 1, int pageSize = 10);
    }
}
