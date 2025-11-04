using BitNow_Backend.DAL.Models;
using BitNow_Backend.DAL.DTOs;

namespace BitNow_Backend.DAL.IRepositories
{
    public interface ICategoryRepository
    {
        Task<IEnumerable<Category>> GetAllAsync();
        Task<PaginatedResult<Category>> GetPagedAsync(CategoryFilterDto filter);
        Task<Category?> GetByIdAsync(int id);
        Task<Category?> GetBySlugAsync(string slug);
        Task<Category> CreateAsync(Category category);
        Task<Category> UpdateAsync(Category category);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
        Task<bool> SlugExistsAsync(string slug, int? excludeId = null);
    }
}
