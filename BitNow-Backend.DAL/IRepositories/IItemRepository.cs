using BitNow_Backend.DAL.Models;

namespace BitNow_Backend.DAL.IRepositories
{
    public interface IItemRepository
    {
        Task<IEnumerable<Item>> GetPagedAsync(int page, int pageSize);
        Task<Item?> GetByIdAsync(int id);
        Task<IEnumerable<Item>> GetBySellerIdAsync(int sellerId, int page, int pageSize);
        Task<int> CountAsync();
        Task<int> CountBySellerIdAsync(int sellerId);

        Task<Item> AddAsync(Item item, Auction auction);

    }
}