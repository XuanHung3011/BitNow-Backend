using Microsoft.AspNetCore.Mvc;
using BitNow_Backend.DAL.DTOs;
using BitNow_Backend.BLL.IServices;

namespace BitNow_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoriesController : ControllerBase
    {
        private readonly ICategoryService _categoryService;

        public CategoriesController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        /// <summary>
        /// Get all categories
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CategoryDto>>> GetAllCategories()
        {
            var categories = await _categoryService.GetAllCategoriesAsync();
            return Ok(categories);
        }

        /// <summary>
        /// Get categories with filtering, sorting and pagination
        /// </summary>
        [HttpGet("paged")]
        public async Task<ActionResult<PaginatedResult<CategoryDto>>> GetCategoriesPaged(
            [FromQuery] string? searchTerm = null,
            [FromQuery] string? sortBy = "Name",
            [FromQuery] string? sortOrder = "asc",
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            // Validate parameters
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 10;
            if (string.IsNullOrEmpty(sortBy)) sortBy = "Name";
            if (string.IsNullOrEmpty(sortOrder)) sortOrder = "asc";

            var filter = new CategoryFilterDto
            {
                SearchTerm = searchTerm,
                SortBy = sortBy,
                SortOrder = sortOrder,
                Page = page,
                PageSize = pageSize
            };

            var result = await _categoryService.GetCategoriesPagedAsync(filter);
            return Ok(result);
        }

        /// <summary>
        /// Get category by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<CategoryDto>> GetCategory(int id)
        {
            var category = await _categoryService.GetCategoryByIdAsync(id);
            if (category == null)
            {
                return NotFound($"Category with ID {id} not found.");
            }
            return Ok(category);
        }

        /// <summary>
        /// Get category by slug
        /// </summary>
        [HttpGet("slug/{slug}")]
        public async Task<ActionResult<CategoryDto>> GetCategoryBySlug(string slug)
        {
            var category = await _categoryService.GetCategoryBySlugAsync(slug);
            if (category == null)
            {
                return NotFound($"Category with slug '{slug}' not found.");
            }
            return Ok(category);
        }

        /// <summary>
        /// Create a new category
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<CategoryDto>> CreateCategory(CreateCategoryDto createCategoryDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var category = await _categoryService.CreateCategoryAsync(createCategoryDto);
                return CreatedAtAction(nameof(GetCategory), new { id = category.Id }, category);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
        }

        /// <summary>
        /// Update an existing category
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<CategoryDto>> UpdateCategory(int id, UpdateCategoryDto updateCategoryDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var category = await _categoryService.UpdateCategoryAsync(id, updateCategoryDto);
                if (category == null)
                {
                    return NotFound($"Category with ID {id} not found.");
                }

                return Ok(category);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
        }

        /// <summary>
        /// Delete a category
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteCategory(int id)
        {
            var result = await _categoryService.DeleteCategoryAsync(id);
            if (!result)
            {
                return NotFound($"Category with ID {id} not found.");
            }

            return NoContent();
        }

        /// <summary>
        /// Check if category exists
        /// </summary>
        [HttpHead("{id}")]
        public async Task<ActionResult> CategoryExists(int id)
        {
            var exists = await _categoryService.CategoryExistsAsync(id);
            return exists ? Ok() : NotFound();
        }

        /// <summary>
        /// Check if slug exists
        /// </summary>
        [HttpGet("check-slug/{slug}")]
        public async Task<ActionResult<bool>> CheckSlugExists(string slug, [FromQuery] int? excludeId = null)
        {
            var exists = await _categoryService.SlugExistsAsync(slug, excludeId);
            return Ok(exists);
        }
    }
}
