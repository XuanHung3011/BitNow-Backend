using BitNow_Backend.BLL.IServices;
using BitNow_Backend.DAL;
using BitNow_Backend.DAL.DTOs;
using BitNow_Backend.DAL.IRepositories;
using BitNow_Backend.DAL.Models;

namespace BitNow_Backend.BLL.Services;

public class RatingService : IRatingService
{
    private readonly IRatingRepository _ratingRepository;
    private readonly IUserRepository _userRepository;
    private readonly BidNowDbContext _context;

    public RatingService(IRatingRepository ratingRepository, IUserRepository userRepository, BidNowDbContext context)
    {
        _ratingRepository = ratingRepository;
        _userRepository = userRepository;
        _context = context;
    }

    public async Task<RatingResponseDto> CreateAsync(RatingCreateDto dto)
    {
        // Validate auction exists and completed, and parties are valid
        var auction = await _context.Auctions.FindAsync(dto.AuctionId);
        if (auction == null)
            throw new InvalidOperationException("Auction not found");

        // Must be between seller and winner
        var sellerId = auction.SellerId;
        var winnerId = auction.WinnerId;
        if (winnerId == null)
            throw new InvalidOperationException("Auction has no winner yet");

        var validPair = (dto.RaterId == sellerId && dto.RatedId == winnerId)
                        || (dto.RaterId == winnerId && dto.RatedId == sellerId);
        if (!validPair)
            throw new InvalidOperationException("Rater and rated must be the seller and winner of the auction");

        // Prevent duplicate (unique index also exists)
        var existing = await _ratingRepository.GetByAuctionAndUsersAsync(dto.AuctionId, dto.RaterId, dto.RatedId);
        if (existing != null)
            throw new InvalidOperationException("You have already rated this user for this auction");

        var rating = new Rating
        {
            AuctionId = dto.AuctionId,
            RaterId = dto.RaterId,
            RatedId = dto.RatedId,
            Rating1 = dto.Rating,
            Comment = dto.Comment,
            CreatedAt = DateTime.UtcNow
        };

        rating = await _ratingRepository.AddAsync(rating);

        // Update aggregates for rated user
        var aggregates = await _ratingRepository.GetAggregateForUserAsync(dto.RatedId);
        var ratedUser = await _userRepository.GetByIdAsync(dto.RatedId);
        if (ratedUser != null)
        {
            ratedUser.TotalRatings = aggregates.Count;
            ratedUser.ReputationScore = aggregates.Average;
            await _userRepository.UpdateAsync(ratedUser);
        }

        return MapToResponseDto(rating);
    }

    public async Task<IEnumerable<RatingResponseDto>> GetForUserAsync(int userId, int page = 1, int pageSize = 10)
    {
        var ratings = await _ratingRepository.GetByRatedUserAsync(userId, page, pageSize);
        return ratings.Select(MapToResponseDto).ToList();
    }

    public async Task<IEnumerable<RatingResponseDto>> GetForAuctionAsync(int auctionId)
    {
        var ratings = await _ratingRepository.GetByAuctionAsync(auctionId);
        return ratings.Select(MapToResponseDto).ToList();
    }

    private static RatingResponseDto MapToResponseDto(Rating rating)
    {
        return new RatingResponseDto
        {
            Id = rating.Id,
            AuctionId = rating.AuctionId,
            RaterId = rating.RaterId,
            RatedId = rating.RatedId,
            Rating = rating.Rating1,
            Comment = rating.Comment,
            CreatedAt = rating.CreatedAt
        };
    }
}


