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

        public async Task<IEnumerable<Item>> GetPagedAsync(int page, int pageSize)
        {
            return await _context.Items
                .Include(i => i.Category)
                .Include(i => i.Seller)
                .OrderByDescending(i => i.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<Item?> GetByIdAsync(int id)
        {
            return await _context.Items
                .Include(i => i.Category)
                .Include(i => i.Seller)
                .Include(i => i.Auctions) 
                .FirstOrDefaultAsync(i => i.Id == id);
        }

        public async Task<IEnumerable<Item>> GetBySellerIdAsync(int sellerId, int page, int pageSize)
        {
            return await _context.Items
                .Where(i => i.SellerId == sellerId)
                .Include(i => i.Category)
                .Include(i => i.Seller)
                .Include(i => i.Auctions)
                .OrderByDescending(i => i.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
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

        public async Task<int> CountAsync()
        {
            return await _context.Items.CountAsync();
        }

        public async Task<int> CountBySellerIdAsync(int sellerId)
        {
            return await _context.Items.CountAsync(i => i.SellerId == sellerId);
        }

        public async Task<Item> AddAsync(Item item, Auction auction)
        {
          
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.Items.Add(item);
                await _context.SaveChangesAsync();
         
                auction.ItemId = item.Id;

                _context.Auctions.Add(auction);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                await _context.Entry(item).Reference(i => i.Category).LoadAsync();
                await _context.Entry(item).Reference(i => i.Seller).LoadAsync();
                await _context.Entry(item).Collection(i => i.Auctions).LoadAsync();

                return item;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
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

        public async Task<IEnumerable<Item>> GetHotApprovedActiveAuctionsAsync(int limit)
        {
            if (limit <= 0) limit = 8;

            // Items approved and with at least one active auction, ordered by activity
            return await _context.Items
                .Include(i => i.Category)
                .Include(i => i.Seller)
                .Include(i => i.Auctions)
                .Where(i => i.Status == "approved" && i.Auctions.Any(a => a.Status == "active"))
                .OrderByDescending(i => i.Auctions
                    .Where(a => a.Status == "active")
                    .Select(a => (int?)a.BidCount)
                    .Max() ?? 0)
                .ThenByDescending(i => i.Auctions
                    .Where(a => a.Status == "active")
                    .Select(a => (decimal?)a.CurrentBid)
                    .Max() ?? 0)
                .ThenBy(i => i.Auctions
                    .Where(a => a.Status == "active")
                    .Select(a => a.EndTime)
                    .Min())
                .Take(limit)
                .ToListAsync();
        }

        public async Task<IEnumerable<Item>> GetAllItemsWithFilterAsync(ItemFilterAllDto filter)
        {
            if (filter.Page <= 0) filter.Page = 1;
            if (filter.PageSize <= 0) filter.PageSize = 10;
            if (filter.PageSize > 100) filter.PageSize = 100;

            var query = _context.Items
                .Include(i => i.Category)
                .Include(i => i.Seller)
                .Include(i => i.Auctions)
                .AsQueryable();

            // Filter by status
            if (filter.Statuses != null && filter.Statuses.Any())
            {
                var normalizedStatuses = filter.Statuses
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Select(s => s.ToLower().Trim())
                    .ToList();

                if (normalizedStatuses.Any())
                {
                    query = query.Where(i => i.Status != null && normalizedStatuses.Contains(i.Status.ToLower()));
                }
            }

            // Filter by category
            if (filter.CategoryId.HasValue && filter.CategoryId.Value > 0)
            {
                query = query.Where(i => i.CategoryId == filter.CategoryId.Value);
            }

            // Filter by sellerId (quan trọng: chỉ hiển thị items của seller đó)
            if (filter.SellerId.HasValue && filter.SellerId.Value > 0)
            {
                query = query.Where(i => i.SellerId == filter.SellerId.Value);
            }

            // Sorting
            var sortBy = (filter.SortBy ?? "CreatedAt").ToLower();
            var sortOrder = (filter.SortOrder ?? "desc").ToLower();

            switch (sortBy)
            {
                case "title":
                    query = sortOrder == "asc" 
                        ? query.OrderBy(i => i.Title) 
                        : query.OrderByDescending(i => i.Title);
                    break;
                case "baseprice":
                    query = sortOrder == "asc" 
                        ? query.OrderBy(i => i.BasePrice) 
                        : query.OrderByDescending(i => i.BasePrice);
                    break;
                case "createdat":
                default:
                    query = sortOrder == "asc" 
                        ? query.OrderBy(i => i.CreatedAt) 
                        : query.OrderByDescending(i => i.CreatedAt);
                    break;
            }

            // Pagination
            var result = await query
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return result;
        }

        public async Task<int> CountAllItemsWithFilterAsync(ItemFilterAllDto filter)
        {
            var query = _context.Items.AsQueryable();

            // Filter by status
            if (filter.Statuses != null && filter.Statuses.Any())
            {
                var normalizedStatuses = filter.Statuses
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Select(s => s.ToLower().Trim())
                    .ToList();

                if (normalizedStatuses.Any())
                {
                    query = query.Where(i => i.Status != null && normalizedStatuses.Contains(i.Status.ToLower()));
                }
            }

            // Filter by category
            if (filter.CategoryId.HasValue && filter.CategoryId.Value > 0)
            {
                query = query.Where(i => i.CategoryId == filter.CategoryId.Value);
            }

            // Filter by sellerId (quan trọng: chỉ đếm items của seller đó)
            if (filter.SellerId.HasValue && filter.SellerId.Value > 0)
            {
                query = query.Where(i => i.SellerId == filter.SellerId.Value);
            }

            return await query.CountAsync();
        }

        public async Task<bool> UpdateItemStatusAsync(int id, string status)
        {
            var item = await _context.Items.FindAsync(id);
            if (item == null)
            {
                return false;
            }

            item.Status = status;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<Item> CreateAsync(Item item)
        {
            _context.Items.Add(item);
            await _context.SaveChangesAsync();
            return item;
        }
    }
}
