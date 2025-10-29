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
            var term = searchTerm.ToLower().Trim();

            return await _context.Items
                .Include(i => i.Category)
                .Include(i => i.Seller)
                .Include(i => i.Auctions)
                .Where(i => i.Status == "approved" &&
                           (i.Title.ToLower().Contains(term) ||
                            i.Category.Name.ToLower().Contains(term)))
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Item>> SearchApprovedWithAuctionPagedAsync(string searchTerm, int page, int pageSize)
        {
            var term = searchTerm.ToLower().Trim();

            return await _context.Items
                .Include(i => i.Category)
                .Include(i => i.Seller)
                .Include(i => i.Auctions)
                .Where(i => i.Status == "approved" &&
                           (i.Title.ToLower().Contains(term) ||
                            i.Category.Name.ToLower().Contains(term)))
                .OrderByDescending(i => i.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> CountSearchApprovedAsync(string searchTerm)
        {
            var term = searchTerm.ToLower().Trim();

            return await _context.Items
                .Where(i => i.Status == "approved" &&
                           (i.Title.ToLower().Contains(term) ||
                            i.Category.Name.ToLower().Contains(term)))
                .CountAsync();
        }
    }
    }
