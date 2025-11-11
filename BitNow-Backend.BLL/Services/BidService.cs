using BitNow_Backend.BLL.IServices;
using BitNow_Backend.DAL;
using BitNow_Backend.DAL.DTOs;
using BitNow_Backend.DAL.IRepositories;
using BitNow_Backend.DAL.Models;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace BitNow_Backend.BLL.Services
{
	public class BidService : IBidService
	{
		private readonly BidNowDbContext _ctx;
		private readonly IAuctionRepository _auctionRepository;
		private readonly IBidRepository _bidRepository;
		private readonly	IConnectionMultiplexer? _redis;

		public BidService(
			BidNowDbContext ctx,
			IAuctionRepository auctionRepository,
			IBidRepository bidRepository,
			IServiceProvider serviceProvider
		)
		{
			_ctx = ctx;
			_auctionRepository = auctionRepository;
			_bidRepository = bidRepository;
			_redis = serviceProvider.GetService<IConnectionMultiplexer>();
		}

		private static string BidsKey(int auctionId) => $"auction:{auctionId}:bids";
		private static string HighestKey(int auctionId) => $"auction:{auctionId}:highest";

		public async Task<BidResultDto> PlaceBidAsync(int auctionId, int bidderId, decimal amount)
		{
			// Validate and persist in DB with optimistic checks
			var auction = await _ctx.Auctions.FirstOrDefaultAsync(a => a.Id == auctionId);
			if (auction == null) throw new InvalidOperationException("Auction not found");
			// Accept 'active' as the running status per DB constraint
			if (!string.Equals(auction.Status, "active", StringComparison.OrdinalIgnoreCase))
			{
				throw new InvalidOperationException("Auction not active");
			}
			if (auction.EndTime <= DateTime.UtcNow) throw new InvalidOperationException("Auction ended");
			if (amount <= auction.CurrentBid || amount < auction.StartingBid) throw new InvalidOperationException("Bid too low");

			// Create bid record
			var bid = new Bid
			{
				AuctionId = auctionId,
				BidderId = bidderId,
				Amount = amount,
				BidTime = DateTime.UtcNow,
				IsAutoBid = false
			};
			await _bidRepository.AddAsync(bid);

			// Update auction current bid and count
			auction.CurrentBid = amount;
			auction.BidCount = (auction.BidCount ?? 0) + 1;
			_ctx.Auctions.Update(auction);
			await _ctx.SaveChangesAsync();

			// Resolve bidder display name
			var bidderUser = await _ctx.Users.FirstOrDefaultAsync(u => u.Id == bidderId);
			var bidderName = bidderUser?.FullName ?? $"User #{bidderId}";

			// Update Redis structures
			if (_redis is not null)
			{
				var db = _redis.GetDatabase();
				var bidJson = JsonSerializer.Serialize(new BidDto
				{
					BidderId = bidderId,
					BidderName = bidderName,
					Amount = amount,
					BidTime = bid.BidTime ?? DateTime.UtcNow
				});
				// Sorted set score by bid time ticks to preserve order; include amount in value
				var ticks = (bid.BidTime ?? DateTime.UtcNow).Ticks;
				_ = await db.SortedSetAddAsync(BidsKey(auctionId), bidJson, ticks);
				// keep only last 100 (remove older)
				var length = await db.SortedSetLengthAsync(BidsKey(auctionId));
				if (length > 100)
				{
					await db.SortedSetRemoveRangeByRankAsync(BidsKey(auctionId), 0, (long)(length - 101));
				}
				// Highest as a simple value
				await db.StringSetAsync(HighestKey(auctionId), amount.ToString());
			}

			return new BidResultDto
			{
				AuctionId = auctionId,
				CurrentBid = auction.CurrentBid ?? amount,
				BidCount = auction.BidCount ?? 0,
				PlacedBid = new BidDto
				{
					BidderId = bidderId,
					BidderName = bidderName,
					Amount = amount,
					BidTime = bid.BidTime ?? DateTime.UtcNow
				}
			};
		}

		public async Task<IReadOnlyList<BidDto>> GetRecentBidsAsync(int auctionId, int limit)
		{
			// Try Redis first
			if (_redis is not null)
			{
				var db = _redis.GetDatabase();
				var entries = await db.SortedSetRangeByRankAsync(BidsKey(auctionId), -limit, -1, StackExchange.Redis.Order.Ascending);
				if (entries?.Length > 0)
				{
					return entries
						.Select(e => JsonSerializer.Deserialize<BidDto>(e!))
						.Where(e => e != null)
						!.Select(e =>
						{
							if (string.IsNullOrWhiteSpace(e!.BidderName))
							{
								e!.BidderName = $"User #{e.BidderId}";
							}
							return e!;
						})
						.ToList()!;
				}
			}
			// Fallback DB
			var list = await _bidRepository.GetRecentByAuctionAsync(auctionId, limit);
			return list.Select(b => new BidDto
			{
				BidderId = b.BidderId,
				BidderName = b.Bidder?.FullName ?? $"User #{b.BidderId}",
				Amount = b.Amount,
				BidTime = b.BidTime ?? DateTime.UtcNow
			}).ToList();
		}

		public async Task<decimal?> GetHighestBidAsync(int auctionId)
		{
			if (_redis is not null)
			{
				var db = _redis.GetDatabase();
				var s = await db.StringGetAsync(HighestKey(auctionId));
				if (s.HasValue && decimal.TryParse(s.ToString(), out var parsed)) return parsed;
			}
			var a = await _auctionRepository.GetByIdAsync(auctionId);
			return a?.CurrentBid;
		}
	}
}


