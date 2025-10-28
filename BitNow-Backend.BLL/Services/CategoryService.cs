using BitNow_Backend.DAL.DTOs;
using BitNow_Backend.DAL.Models;
using BitNow_Backend.DAL.IRepositories;
using BitNow_Backend.BLL.IServices;

namespace BitNow_Backend.BLL.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly ICategoryRepository _categoryRepository;

        public CategoryService(ICategoryRepository categoryRepository)
        {
            _categoryRepository = categoryRepository;
        }

        public async Task<IEnumerable<CategoryDto>> GetAllCategoriesAsync()
        {
            var categories = await _categoryRepository.GetAllAsync();
            return categories.Select(MapToDto);
        }

        public async Task<PaginatedResult<CategoryDto>> GetCategoriesPagedAsync(CategoryFilterDto filter)
        {
            var result = await _categoryRepository.GetPagedAsync(filter);
            return new PaginatedResult<CategoryDto>
            {
                Data = result.Data.Select(MapToDto),
                TotalCount = result.TotalCount,
                Page = result.Page,
                PageSize = result.PageSize
            };
        }

        public async Task<CategoryDto?> GetCategoryByIdAsync(int id)
        {
            var category = await _categoryRepository.GetByIdAsync(id);
            return category != null ? MapToDto(category) : null;
        }

        public async Task<CategoryDto?> GetCategoryBySlugAsync(string slug)
        {
            var category = await _categoryRepository.GetBySlugAsync(slug);
            return category != null ? MapToDto(category) : null;
        }

        public async Task<CategoryDto> CreateCategoryAsync(CreateCategoryDto createCategoryDto)
        {
            // Check if slug already exists
            if (await _categoryRepository.SlugExistsAsync(createCategoryDto.Slug))
            {
                throw new InvalidOperationException($"Category with slug '{createCategoryDto.Slug}' already exists.");
            }

            var category = new Category
            {
                Name = createCategoryDto.Name,
                Slug = createCategoryDto.Slug,
                Description = createCategoryDto.Description,
                Icon = createCategoryDto.Icon,
                CreatedAt = DateTime.UtcNow
            };

            var createdCategory = await _categoryRepository.CreateAsync(category);
            return MapToDto(createdCategory);
        }

        public async Task<CategoryDto?> UpdateCategoryAsync(int id, UpdateCategoryDto updateCategoryDto)
        {
            var category = await _categoryRepository.GetByIdAsync(id);
            if (category == null)
                return null;

            // Check if slug already exists (excluding current category)
            if (await _categoryRepository.SlugExistsAsync(updateCategoryDto.Slug, id))
            {
                throw new InvalidOperationException($"Category with slug '{updateCategoryDto.Slug}' already exists.");
            }

            category.Name = updateCategoryDto.Name;
            category.Slug = updateCategoryDto.Slug;
            category.Description = updateCategoryDto.Description;
            category.Icon = updateCategoryDto.Icon;

            var updatedCategory = await _categoryRepository.UpdateAsync(category);
            return MapToDto(updatedCategory);
        }

        public async Task<bool> DeleteCategoryAsync(int id)
        {
            return await _categoryRepository.DeleteAsync(id);
        }

        public async Task<bool> CategoryExistsAsync(int id)
        {
            return await _categoryRepository.ExistsAsync(id);
        }

        public async Task<bool> SlugExistsAsync(string slug, int? excludeId = null)
        {
            return await _categoryRepository.SlugExistsAsync(slug, excludeId);
        }

        private static CategoryDto MapToDto(Category category)
        {
            return new CategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                Slug = category.Slug,
                Description = category.Description,
                Icon = category.Icon,
                CreatedAt = category.CreatedAt
            };
        }
    }
}
