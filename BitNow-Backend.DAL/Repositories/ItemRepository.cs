using BitNow_Backend.DAL.DTOs;
using BitNow_Backend.DAL.IRepositories;
using BitNow_Backend.DAL.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitNow_Backend.DAL.Repositories
{
    public class ItemRepository : IItemRepository
    {
        private readonly BidNowDbContext _context;

        public ItemRepository(BidNowDbContext context)
        {
            _context = context;
        }

        public async Task<Item?> GetByIdAsync(int id)
        {
            return await _context.Items
                .Include(i => i.Category)
                .Include(i => i.Seller)
                .Include(i => i.Auctions)
                .FirstOrDefaultAsync(i => i.Id == id);
        }

        public async Task<IEnumerable<Item>> GetAllApprovedWithAuctionAsync()
        {
            return await _context.Items
                .Include(i => i.Category)
                .Include(i => i.Seller)
                .Include(i => i.Auctions)
                .Where(i => i.Status == "approved")
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Item>> GetAllApprovedWithAuctionPagedAsync(int page, int pageSize)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 10;

            return await _context.Items
                .Include(i => i.Category)
                .Include(i => i.Seller)
                .Include(i => i.Auctions)
                .Where(i => i.Status == "approved")
                .OrderByDescending(i => i.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> CountApprovedAsync()
        {
            return await _context.Items
                .Where(i => i.Status == "approved")
                .CountAsync();
        }

        public async Task<IEnumerable<Item>> SearchApprovedWithAuctionAsync(string searchTerm)
        {
            var term = (searchTerm ?? string.Empty).ToLower().Trim();

            return await _context.Items
                .Include(i => i.Category)
                .Include(i => i.Seller)
                .Include(i => i.Auctions)
                .Where(i => i.Status == "approved" &&
                           (EF.Functions.Like(i.Title.ToLower(), $"%{term}%") ||
                            (i.Category != null && EF.Functions.Like(i.Category.Name.ToLower(), $"%{term}%"))))
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Item>> SearchApprovedWithAuctionPagedAsync(string searchTerm, int page, int pageSize)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 10;

            var term = (searchTerm ?? string.Empty).ToLower().Trim();

            return await _context.Items
                .Include(i => i.Category)
                .Include(i => i.Seller)
                .Include(i => i.Auctions)
                .Where(i => i.Status == "approved" &&
                           (EF.Functions.Like(i.Title.ToLower(), $"%{term}%") ||
                            (i.Category != null && EF.Functions.Like(i.Category.Name.ToLower(), $"%{term}%"))))
                .OrderByDescending(i => i.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> CountSearchApprovedAsync(string searchTerm)
        {
            var term = (searchTerm ?? string.Empty).ToLower().Trim();

            return await _context.Items
                .Where(i => i.Status == "approved" &&
                           (EF.Functions.Like(i.Title.ToLower(), $"%{term}%") ||
                            (i.Category != null && EF.Functions.Like(i.Category.Name.ToLower(), $"%{term}%"))))
                .CountAsync();
        }

        // New: Advanced Filter implementation
        public async Task<IEnumerable<Item>> FilterApprovedItemsAsync(ItemFilterDto filter, int page, int pageSize)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 10;

            var query = _context.Items
                .Include(i => i.Category)
                .Include(i => i.Seller)
                .Include(i => i.Auctions)
                .Where(i => i.Status == "approved")
                .AsQueryable();

            // Search term (title or category name)
            if (!string.IsNullOrWhiteSpace(filter?.SearchTerm))
            {
                var term = filter.SearchTerm.ToLower().Trim();
                query = query.Where(i =>
                    EF.Functions.Like(i.Title.ToLower(), $"%{term}%") ||
                    (i.Category != null && EF.Functions.Like(i.Category.Name.ToLower(), $"%{term}%")));
            }

            // Category IDs (multiple)
            if (filter?.CategoryIds != null && filter.CategoryIds.Any())
            {
                query = query.Where(i => i.CategoryId != 0 && filter.CategoryIds.Contains(i.CategoryId));
            }

            // Price filters (apply to BasePrice if present)
            if (filter?.MinPrice != null)
            {
                query = query.Where(i => i.BasePrice != null && i.BasePrice >= filter.MinPrice.Value);
            }

            if (filter?.MaxPrice != null)
            {
                query = query.Where(i => i.BasePrice != null && i.BasePrice <= filter.MaxPrice.Value);
            }

            // Condition
            if (!string.IsNullOrWhiteSpace(filter?.Condition))
            {
                var cond = filter.Condition.ToLower().Trim();
                query = query.Where(i => i.Condition != null && i.Condition.ToLower().Contains(cond));
            }

            // Auction statuses: check any related auction with matching status (case-insensitive)
            if (filter?.AuctionStatuses != null && filter.AuctionStatuses.Any())
            {
                var normalizedStatuses = filter.AuctionStatuses
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Select(s => s.ToLower().Trim())
                    .ToList();

                if (normalizedStatuses.Any())
                {
                    query = query.Where(i =>
                        i.Auctions.Any(a => a.Status != null && normalizedStatuses.Contains(a.Status.ToLower())));
                }
            }

            // Ordering and paging
            var result = await query
                .OrderByDescending(i => i.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return result;
        }

        public async Task<int> CountFilteredApprovedAsync(ItemFilterDto filter)
        {
            var query = _context.Items
                .Where(i => i.Status == "approved")
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter?.SearchTerm))
            {
                var term = filter.SearchTerm.ToLower().Trim();
                query = query.Where(i =>
                    EF.Functions.Like(i.Title.ToLower(), $"%{term}%") ||
                    (i.Category != null && EF.Functions.Like(i.Category.Name.ToLower(), $"%{term}%")));
            }

            if (filter?.CategoryIds != null && filter.CategoryIds.Any())
            {
                query = query.Where(i => i.CategoryId != 0 && filter.CategoryIds.Contains(i.CategoryId));
            }

            if (filter?.MinPrice != null)
            {
                query = query.Where(i => i.BasePrice != null && i.BasePrice >= filter.MinPrice.Value);
            }

            if (filter?.MaxPrice != null)
            {
                query = query.Where(i => i.BasePrice != null && i.BasePrice <= filter.MaxPrice.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter?.Condition))
            {
                var cond = filter.Condition.ToLower().Trim();
                query = query.Where(i => i.Condition != null && i.Condition.ToLower().Contains(cond));
            }

            if (filter?.AuctionStatuses != null && filter.AuctionStatuses.Any())
            {
                var normalizedStatuses = filter.AuctionStatuses
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Select(s => s.ToLower().Trim())
                    .ToList();

                if (normalizedStatuses.Any())
                {
                    query = query.Where(i =>
                        i.Auctions.Any(a => a.Status != null && normalizedStatuses.Contains(a.Status.ToLower())));
                }
            }

            return await query.CountAsync();
        }
        public async Task<IEnumerable<CategoryDto>> GetCategoriesAsync()
        {
            return await _context.Categories
                .OrderBy(c => c.Name)
                .Select(c => new CategoryDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Slug = c.Slug,
                    Icon = c.Icon
                })
                .ToListAsync();
        }
    }
}
