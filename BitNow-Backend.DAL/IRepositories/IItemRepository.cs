using BitNow_Backend.DAL.DTOs;
using BitNow_Backend.DAL.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

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

        // Extended queries
        Task<IEnumerable<Item>> GetAllApprovedWithAuctionAsync();
        Task<IEnumerable<Item>> GetAllApprovedWithAuctionPagedAsync(int page, int pageSize);
        Task<int> CountApprovedAsync();
        Task<IEnumerable<Item>> SearchApprovedWithAuctionAsync(string searchTerm);
        Task<IEnumerable<Item>> SearchApprovedWithAuctionPagedAsync(string searchTerm, int page, int pageSize);
        Task<int> CountSearchApprovedAsync(string searchTerm);
        Task<IEnumerable<Item>> FilterApprovedItemsAsync(ItemFilterDto filter, int page, int pageSize);
        Task<int> CountFilteredApprovedAsync(ItemFilterDto filter);
        Task<IEnumerable<CategoryDto>> GetCategoriesAsync();
        Task<IEnumerable<Item>> GetHotApprovedActiveAuctionsAsync(int limit);

        // Get all items with filtering and sorting (not just approved)
        Task<IEnumerable<Item>> GetAllItemsWithFilterAsync(ItemFilterAllDto filter);
        Task<int> CountAllItemsWithFilterAsync(ItemFilterAllDto filter);

        // Update item status
        Task<bool> UpdateItemStatusAsync(int id, string status);
    }
}
