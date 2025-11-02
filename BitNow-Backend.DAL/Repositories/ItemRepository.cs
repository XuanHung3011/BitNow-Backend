using BitNow_Backend.DAL.IRepositories;
using BitNow_Backend.DAL.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
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
    }
}