using BitNow_Backend.DAL.DTOs;

namespace BitNow_Backend.BLL.IServices;

public interface ISellerStatsService
{
    Task<SellerStatsDto> GetSellerStatsAsync(int sellerId);
    Task<SellerStatsDetailDto> GetSellerStatsDetailAsync(int sellerId, string type);
}

