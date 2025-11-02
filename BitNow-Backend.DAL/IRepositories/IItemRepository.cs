using BitNow_Backend.DAL.DTOs;
using BitNow_Backend.DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitNow_Backend.DAL.IRepositories
{
    public interface IItemRepository
    {
        Task<Item?> GetByIdAsync(int id);
        Task<IEnumerable<Item>> GetAllApprovedWithAuctionAsync();
        Task<IEnumerable<Item>> GetAllApprovedWithAuctionPagedAsync(int page, int pageSize);
        Task<int> CountApprovedAsync();
        Task<IEnumerable<Item>> SearchApprovedWithAuctionAsync(string searchTerm);
        Task<IEnumerable<Item>> SearchApprovedWithAuctionPagedAsync(string searchTerm, int page, int pageSize);
        Task<int> CountSearchApprovedAsync(string searchTerm);

        // New: Advanced Filter
        Task<IEnumerable<Item>> FilterApprovedItemsAsync(ItemFilterDto filter, int page, int pageSize);
        Task<int> CountFilteredApprovedAsync(ItemFilterDto filter);

        Task<IEnumerable<CategoryDto>> GetCategoriesAsync();
    }
}
