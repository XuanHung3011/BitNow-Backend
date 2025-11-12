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
    public class FavoriteSellerRepository : IFavoriteSellerRepository
    {
        private readonly BidNowDbContext _context;

        public FavoriteSellerRepository(BidNowDbContext context)
        {
            _context = context;
        }

        public async Task<FavoriteSeller?> GetByBuyerAndSellerAsync(int buyerId, int sellerId)
        {
            return await _context.FavoriteSellers
                .Include(fs => fs.Seller)
                .FirstOrDefaultAsync(fs => fs.BuyerId == buyerId && fs.SellerId == sellerId);
        }

        public async Task<List<FavoriteSeller>> GetByBuyerIdAsync(int buyerId)
        {
            return await _context.FavoriteSellers
                .Include(fs => fs.Seller)
                .Where(fs => fs.BuyerId == buyerId)
                .OrderByDescending(fs => fs.CreatedAt)
                .ToListAsync();
        }

        public async Task<FavoriteSeller> AddAsync(FavoriteSeller favoriteSeller)
        {
            await _context.FavoriteSellers.AddAsync(favoriteSeller);
            await _context.SaveChangesAsync();

            // Load Seller info
            await _context.Entry(favoriteSeller)
                .Reference(fs => fs.Seller)
                .LoadAsync();

            return favoriteSeller;
        }

        public async Task<bool> DeleteAsync(int buyerId, int sellerId)
        {
            var favorite = await _context.FavoriteSellers
                .FirstOrDefaultAsync(fs => fs.BuyerId == buyerId && fs.SellerId == sellerId);

            if (favorite == null) return false;

            _context.FavoriteSellers.Remove(favorite);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(int buyerId, int sellerId)
        {
            return await _context.FavoriteSellers
                .AnyAsync(fs => fs.BuyerId == buyerId && fs.SellerId == sellerId);
        }
    }
}
