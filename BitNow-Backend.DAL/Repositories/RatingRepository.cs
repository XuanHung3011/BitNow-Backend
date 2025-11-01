using BitNow_Backend.DAL.IRepositories;
using BitNow_Backend.DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace BitNow_Backend.DAL.Repositories;

public class RatingRepository : IRatingRepository
{
    private readonly BidNowDbContext _context;

    public RatingRepository(BidNowDbContext context)
    {
        _context = context;
    }

    public async Task<Rating?> GetByAuctionAndUsersAsync(int auctionId, int raterId, int ratedId)
    {
        return await _context.Ratings.FirstOrDefaultAsync(r => r.AuctionId == auctionId && r.RaterId == raterId && r.RatedId == ratedId);
    }

    public async Task<Rating> AddAsync(Rating rating)
    {
        _context.Ratings.Add(rating);
        await _context.SaveChangesAsync();
        return rating;
    }

    public async Task<IEnumerable<Rating>> GetByRatedUserAsync(int ratedUserId, int page, int pageSize)
    {
        return await _context.Ratings
            .Where(r => r.RatedId == ratedUserId)
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<(int Count, decimal? Average)> GetAggregateForUserAsync(int ratedUserId)
    {
        var query = _context.Ratings.Where(r => r.RatedId == ratedUserId);
        var count = await query.CountAsync();
        decimal? avg = null;
        if (count > 0)
        {
            avg = (decimal?)await query.AverageAsync(r => r.Rating1);
        }
        return (count, avg);
    }

    public async Task<IEnumerable<Rating>> GetByAuctionAsync(int auctionId)
    {
        return await _context.Ratings.Where(r => r.AuctionId == auctionId).ToListAsync();
    }
}


