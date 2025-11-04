using Microsoft.EntityFrameworkCore;
using BitNow_Backend.DAL.Models;
using BitNow_Backend.DAL.IRepositories;
using BitNow_Backend.DAL.DTOs;

namespace BitNow_Backend.DAL.Repositories
{
    public class CategoryRepository : ICategoryRepository
    {
        private readonly BidNowDbContext _context;

        public CategoryRepository(BidNowDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Category>> GetAllAsync()
        {
            return await _context.Categories
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<PaginatedResult<Category>> GetPagedAsync(CategoryFilterDto filter)
        {
            var query = _context.Categories.AsQueryable();

            // Apply search filter
            if (!string.IsNullOrEmpty(filter.SearchTerm))
            {
                query = query.Where(c => c.Name.Contains(filter.SearchTerm) || 
                                       c.Description.Contains(filter.SearchTerm));
            }

            // Apply sorting
            query = filter.SortBy?.ToLower() switch
            {
                "name" => filter.SortOrder?.ToLower() == "desc" 
                    ? query.OrderByDescending(c => c.Name)
                    : query.OrderBy(c => c.Name),
                "createdat" => filter.SortOrder?.ToLower() == "desc"
                    ? query.OrderByDescending(c => c.CreatedAt)
                    : query.OrderBy(c => c.CreatedAt),
                _ => query.OrderBy(c => c.Name)
            };

            // Get total count before pagination
            var totalCount = await query.CountAsync();

            // Apply pagination
            var items = await query
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return new PaginatedResult<Category>
            {
                Data = items,
                TotalCount = totalCount,
                Page = filter.Page,
                PageSize = filter.PageSize
            };
        }

        public async Task<Category?> GetByIdAsync(int id)
        {
            return await _context.Categories
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<Category?> GetBySlugAsync(string slug)
        {
            return await _context.Categories
                .FirstOrDefaultAsync(c => c.Slug == slug);
        }

        public async Task<Category> CreateAsync(Category category)
        {
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();
            return category;
        }

        public async Task<Category> UpdateAsync(Category category)
        {
            _context.Categories.Update(category);
            await _context.SaveChangesAsync();
            return category;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
                return false;

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.Categories
                .AnyAsync(c => c.Id == id);
        }

        public async Task<bool> SlugExistsAsync(string slug, int? excludeId = null)
        {
            var query = _context.Categories.Where(c => c.Slug == slug);
            
            if (excludeId.HasValue)
            {
                query = query.Where(c => c.Id != excludeId.Value);
            }

            return await query.AnyAsync();
        }
    }
}
