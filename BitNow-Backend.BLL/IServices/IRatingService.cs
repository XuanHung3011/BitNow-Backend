using BitNow_Backend.DAL.DTOs;

namespace BitNow_Backend.BLL.IServices;

public interface IRatingService
{
    Task<RatingResponseDto> CreateAsync(RatingCreateDto dto);
    Task<IEnumerable<RatingResponseDto>> GetForUserAsync(int userId, int page = 1, int pageSize = 10);
    Task<IEnumerable<RatingResponseDto>> GetForAuctionAsync(int auctionId);
}


