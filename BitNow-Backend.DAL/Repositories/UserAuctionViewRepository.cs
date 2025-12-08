using BitNow_Backend.DAL.IRepositories;
using BitNow_Backend.DAL.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BitNow_Backend.DAL.Repositories
{
    public class UserAuctionViewRepository : IUserAuctionViewRepository
    {
        private readonly BidNowDbContext _context;

        public UserAuctionViewRepository(BidNowDbContext context)
        {
            _context = context;
        }

        public async Task<UserAuctionView?> GetLastViewAsync(int userId, int auctionId)
        {
            return await _context.UserAuctionViews
                .AsNoTracking()
                .Where(v => v.UserId == userId && v.AuctionId == auctionId)
                .OrderByDescending(v => v.ViewedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<List<UserAuctionView>> GetRecentByUserAsync(int userId, int take = 20)
        {
            return await _context.UserAuctionViews
                .AsNoTracking()
                .Where(v => v.UserId == userId)
                .OrderByDescending(v => v.ViewedAt)
                .Take(take)
                .ToListAsync();
        }

        public async Task AddAsync(UserAuctionView view)
        {
            await _context.UserAuctionViews.AddAsync(view);
            await _context.SaveChangesAsync();
        }
        public async Task<int> DeleteOldViewsAsync(DateTime olderThan)
        {
            var rowsDeleted = await _context.UserAuctionViews
                .Where(v => v.ViewedAt < olderThan)
                .ExecuteDeleteAsync(); 

            return rowsDeleted;
        }
    }
}

