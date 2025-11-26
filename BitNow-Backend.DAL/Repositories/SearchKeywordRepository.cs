using BitNow_Backend.DAL.IRepositories;
using BitNow_Backend.DAL.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

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
}
