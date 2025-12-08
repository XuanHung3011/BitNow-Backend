using BitNow_Backend.DAL.IRepositories;
using BitNow_Backend.DAL.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BitNow_Backend.DAL.Repositories
{
    public class SearchKeywordRepository : ISearchKeywordRepository
    {
        private readonly BidNowDbContext _context;

        public SearchKeywordRepository(BidNowDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(SearchKeyword keyword)
        {
            _context.SearchKeywords.Add(keyword);
            await _context.SaveChangesAsync();
        }

        public async Task<List<SearchKeyword>> GetRecentByUserAsync(int userId, int take = 20)
        {
            return await _context.SearchKeywords
                .AsNoTracking()
                .Where(k => k.UserId == userId)
                .OrderByDescending(k => k.CreatedAt)
                .Take(take)
                .ToListAsync();
        }


        /// Xóa tất cả keywords có CreatedAt cũ hơn cutoffDate
        public async Task<int> DeleteOlderThanAsync(DateTime cutoffDate, CancellationToken cancellationToken = default)
        {

            var deletedCount = await _context.SearchKeywords
                .Where(k => k.CreatedAt < cutoffDate)
                .ExecuteDeleteAsync(cancellationToken);

            return deletedCount;
        }
    }
}