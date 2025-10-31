using BitNow_Backend.DAL.DTOs;

namespace BitNow_Backend.BLL.IServices
{
    public interface ICategoryService
    {
        Task<IEnumerable<CategoryDto>> GetAllCategoriesAsync();
        Task<PaginatedResult<CategoryDto>> GetCategoriesPagedAsync(CategoryFilterDto filter);
        Task<CategoryDto?> GetCategoryByIdAsync(int id);
        Task<CategoryDto?> GetCategoryBySlugAsync(string slug);
        Task<CategoryDto> CreateCategoryAsync(CreateCategoryDto createCategoryDto);
        Task<CategoryDto?> UpdateCategoryAsync(int id, UpdateCategoryDto updateCategoryDto);
        Task<bool> DeleteCategoryAsync(int id);
        Task<bool> CategoryExistsAsync(int id);
        Task<bool> SlugExistsAsync(string slug, int? excludeId = null);
    }
}
