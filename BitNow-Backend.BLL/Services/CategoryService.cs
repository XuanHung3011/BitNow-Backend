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

        public async Task<IEnumerable<CategoryDtos>> GetAllCategoriesAsync()
        {
            var categories = await _categoryRepository.GetAllAsync();
            return categories.Select(MapToDto);
        }

        public async Task<PaginatedResult<CategoryDtos>> GetCategoriesPagedAsync(CategoryFilterDto filter)
        {
            var result = await _categoryRepository.GetPagedAsync(filter);
            return new PaginatedResult<CategoryDtos>
            {
                Data = result.Data.Select(MapToDto),
                TotalCount = result.TotalCount,
                Page = result.Page,
                PageSize = result.PageSize
            };
        }

        public async Task<CategoryDtos?> GetCategoryByIdAsync(int id)
        {
            var category = await _categoryRepository.GetByIdAsync(id);
            return category != null ? MapToDto(category) : null;
        }

        public async Task<CategoryDtos?> GetCategoryBySlugAsync(string slug)
        {
            var category = await _categoryRepository.GetBySlugAsync(slug);
            return category != null ? MapToDto(category) : null;
        }

        public async Task<CategoryDtos> CreateCategoryAsync(CreateCategoryDtos createCategoryDtos)
        {
            // Check if slug already exists
            if (await _categoryRepository.SlugExistsAsync(createCategoryDtos.Slug))
            {
                throw new InvalidOperationException($"Category with slug '{createCategoryDtos.Slug}' already exists.");
            }

            var category = new Category
            {
                Name = createCategoryDtos.Name,
                Slug = createCategoryDtos.Slug,
                Description = createCategoryDtos.Description,
                Icon = createCategoryDtos.Icon,
                CreatedAt = DateTime.UtcNow
            };

            var createdCategory = await _categoryRepository.CreateAsync(category);
            return MapToDto(createdCategory);
        }

        public async Task<CategoryDtos?> UpdateCategoryAsync(int id, UpdateCategoryDtos updateCategoryDtos)
        {
            var category = await _categoryRepository.GetByIdAsync(id);
            if (category == null)
                return null;

            // Check if slug already exists (excluding current category)
            if (await _categoryRepository.SlugExistsAsync(updateCategoryDtos.Slug, id))
            {
                throw new InvalidOperationException($"Category with slug '{updateCategoryDtos.Slug}' already exists.");
            }

            category.Name = updateCategoryDtos.Name;
            category.Slug = updateCategoryDtos.Slug;
            category.Description = updateCategoryDtos.Description;
            category.Icon = updateCategoryDtos.Icon;

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

        private static CategoryDtos MapToDto(Category category)
        {
            return new CategoryDtos
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
