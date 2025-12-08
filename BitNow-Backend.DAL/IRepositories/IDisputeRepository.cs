using BitNow_Backend.DAL.Models;

namespace BitNow_Backend.DAL.IRepositories;

public interface IDisputeRepository
{
    Task<Dispute?> GetByIdAsync(int id);
    Task<Dispute?> GetByOrderIdAsync(int orderId);
    Task<IEnumerable<Dispute>> GetAllAsync();
    Task<IEnumerable<Dispute>> GetByStatusAsync(string status);
    Task<IEnumerable<Dispute>> GetByBuyerIdAsync(int buyerId);
    Task<IEnumerable<Dispute>> GetBySellerIdAsync(int sellerId);
    Task<Dispute> AddAsync(Dispute dispute);
    Task UpdateAsync(Dispute dispute);
    Task DeleteAsync(Dispute dispute);
}

