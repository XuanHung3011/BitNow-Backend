using BitNow_Backend.DAL.DTOs;

namespace BitNow_Backend.BLL.IServices;

public interface IDisputeService
{
    Task<DisputeDto?> GetByIdAsync(int id);
    Task<DisputeDto?> GetByOrderIdAsync(int orderId);
    Task<IEnumerable<DisputeDto>> GetAllAsync();
    Task<IEnumerable<DisputeDto>> GetByStatusAsync(string status);
    Task<IEnumerable<DisputeDto>> GetByBuyerIdAsync(int buyerId);
    Task<IEnumerable<DisputeDto>> GetBySellerIdAsync(int sellerId);
    Task<DisputeDto> CreateDisputeAsync(CreateDisputeDto dto, int buyerId);
    Task<DisputeDto> StartReviewAsync(int disputeId, int adminId);
    Task<DisputeDto> ResolveDisputeAsync(int disputeId, ResolveDisputeDto dto, int adminId);
}

