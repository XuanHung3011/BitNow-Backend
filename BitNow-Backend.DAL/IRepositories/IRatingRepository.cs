using BitNow_Backend.DAL.Models;

namespace BitNow_Backend.DAL.IRepositories;

public interface IRatingRepository
{
    Task<Rating?> GetByAuctionAndUsersAsync(int auctionId, int raterId, int ratedId);
    Task<Rating> AddAsync(Rating rating);
    Task<IEnumerable<Rating>> GetByRatedUserAsync(int ratedUserId, int page, int pageSize);
    Task<(int Count, decimal? Average)> GetAggregateForUserAsync(int ratedUserId);
    Task<IEnumerable<Rating>> GetByAuctionAsync(int auctionId);
}


