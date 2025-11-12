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
	/// <summary>
	/// Service xử lý đặt giá và đọc lịch sử đấu giá.
	/// Redis được dùng làm lớp cache:
	///  - String key  : "auction:{id}:highest" lưu giá cao nhất hiện tại.
	///  - Sorted set  : "auction:{id}:bids"   lưu tối đa 100 lượt đặt giá gần nhất (giá & thời gian).
	/// Khi cache trống hoặc Redis không sẵn sàng, hệ thống tự động fallback về SQL.
	/// </summary>
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

			// Lấy tên hiển thị của người đặt giá để cache kèm (giúp FE không phải gọi thêm API khác).
			var bidderUser = await _ctx.Users.FirstOrDefaultAsync(u => u.Id == bidderId);
			var bidderName = bidderUser?.FullName ?? $"User #{bidderId}";

			// Ghi vào Redis: vừa append lịch sử, vừa cập nhật giá cao nhất.
			// Nếu Redis unavailable, khối này bị bỏ qua -> dữ liệu vẫn nằm trong SQL.
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
				// Sorted set: score = ticks để đảm bảo trật tự thời gian tăng dần.
				var ticks = (bid.BidTime ?? DateTime.UtcNow).Ticks;
				_ = await db.SortedSetAddAsync(BidsKey(auctionId), bidJson, ticks);
				// Giữ tối đa 100 bản ghi mới nhất, remove phần thừa phía đầu.
				var length = await db.SortedSetLengthAsync(BidsKey(auctionId));
				if (length > 100)
				{
					await db.SortedSetRemoveRangeByRankAsync(BidsKey(auctionId), 0, (long)(length - 101));
				}
				// Giá cao nhất được lưu dưới dạng string để truy xuất cực nhanh.
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
			// Ưu tiên đọc từ Redis (cache nóng) để tránh query SQL liên tục.
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
			// Cache trống hoặc Redis không sẵn sàng => fallback đọc trực tiếp từ SQL.
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


