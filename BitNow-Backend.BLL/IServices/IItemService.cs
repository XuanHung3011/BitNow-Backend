using BitNow_Backend.DAL.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitNow_Backend.BLL.IServices
{
    public interface IItemService
    {
        Task<IEnumerable<ItemResponseDto>> GetAllApprovedItemsAsync();
        Task<IEnumerable<ItemResponseDto>> GetAllApprovedItemsPagedAsync(int page, int pageSize);
        Task<(IEnumerable<ItemResponseDto> items, int totalCount)> GetAllApprovedItemsWithCountAsync(int page, int pageSize);
        Task<IEnumerable<ItemResponseDto>> SearchApprovedItemsAsync(string searchTerm);
        Task<IEnumerable<ItemResponseDto>> SearchApprovedItemsPagedAsync(string searchTerm, int page, int pageSize);
        Task<(IEnumerable<ItemResponseDto> items, int totalCount)> SearchApprovedItemsWithCountAsync(string searchTerm, int page, int pageSize);

        // New: Advanced Filter
        Task<(IEnumerable<ItemResponseDto> items, int totalCount)> FilterApprovedItemsAsync(ItemFilterDto filter, int page, int pageSize);
        Task<IEnumerable<CategoryDto>> GetCategoriesAsync();

        // Hot
        Task<IEnumerable<ItemResponseDto>> GetHotApprovedItemsAsync(int limit);
    }
}
