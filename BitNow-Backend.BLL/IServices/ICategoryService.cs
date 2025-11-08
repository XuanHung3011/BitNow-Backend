using BitNow_Backend.DAL.DTOs;

namespace BitNow_Backend.BLL.IServices
{
    public interface ICategoryService
    {
        Task<IEnumerable<CategoryDtos>> GetAllCategoriesAsync();
        Task<PaginatedResult<CategoryDtos>> GetCategoriesPagedAsync(CategoryFilterDto filter);
        Task<CategoryDtos?> GetCategoryByIdAsync(int id);
        Task<CategoryDtos?> GetCategoryBySlugAsync(string slug);
        Task<CategoryDtos> CreateCategoryAsync(CreateCategoryDtos createCategoryDtos);
        Task<CategoryDtos?> UpdateCategoryAsync(int id, UpdateCategoryDtos updateCategoryDtos);
        Task<bool> DeleteCategoryAsync(int id);
        Task<bool> CategoryExistsAsync(int id);
        Task<bool> SlugExistsAsync(string slug, int? excludeId = null);
        Task<bool> NameExistsAsync(string name, int? excludeId = null);
        Task<bool> IsCategoryInUseAsync(int id);
    }
}
