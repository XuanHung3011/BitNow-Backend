using BitNow_Backend.DAL.IRepositories;
using BitNow_Backend.DAL.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace BitNow_Backend.DAL.Repositories;

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
}
