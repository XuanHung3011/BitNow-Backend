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
		private readonly IServiceScopeFactory _serviceScopeFactory;
		private readonly IBidNotificationService? _bidNotificationService;
		private readonly INotificationService? _notificationService;

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
			_serviceScopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
			_bidNotificationService = serviceProvider.GetService<IBidNotificationService>();
			_notificationService = serviceProvider.GetService<INotificationService>();
		}

		private static string BidsKey(int auctionId) => $"auction:{auctionId}:bids";
		private static string HighestKey(int auctionId) => $"auction:{auctionId}:highest";

		public async Task<BidResultDto> PlaceBidAsync(int auctionId, int bidderId, decimal amount, bool isAutoBid = false)
		{
			// Validate and persist in DB with optimistic checks
			var auction = await _ctx.Auctions
				.Include(a => a.Item)
				.FirstOrDefaultAsync(a => a.Id == auctionId);
			if (auction == null) throw new InvalidOperationException("Auction not found");
			// Accept 'active' as the running status per DB constraint
			if (!string.Equals(auction.Status, "active", StringComparison.OrdinalIgnoreCase))
			{
				throw new InvalidOperationException("Auction not active");
			}
			if (auction.EndTime <= DateTime.Now) throw new InvalidOperationException("Auction ended");
			if (amount <= auction.CurrentBid || amount < auction.StartingBid) throw new InvalidOperationException("Bid too low");

			// Lấy bid cao nhất trước đó để thông báo outbid (nếu có)
			var previousHighestBid = await _ctx.Bids
				.Where(b => b.AuctionId == auctionId)
				.OrderByDescending(b => b.Amount)
				.ThenByDescending(b => b.BidTime)
				.FirstOrDefaultAsync();

			// Create bid record
			var bid = new Bid
			{
				AuctionId = auctionId,
				BidderId = bidderId,
				Amount = amount,
				BidTime = DateTime.Now,
				IsAutoBid = isAutoBid
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
					BidTime = bid.BidTime ?? DateTime.Now
				});
				// Sorted set: score = ticks để đảm bảo trật tự thời gian tăng dần.
				var ticks = (bid.BidTime ?? DateTime.Now).Ticks;
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

			var result = new BidResultDto
			{
				AuctionId = auctionId,
				CurrentBid = auction.CurrentBid ?? amount,
				BidCount = auction.BidCount ?? 0,
				PlacedBid = new BidDto
				{
					BidderId = bidderId,
					BidderName = bidderName,
					Amount = amount,
					BidTime = bid.BidTime ?? DateTime.Now
				}
			};

			// Nếu có bidder trước đó và khác bidder hiện tại -> gửi thông báo bị vượt giá
			try
			{
				if (previousHighestBid != null
					&& previousHighestBid.BidderId != bidderId
					&& amount > previousHighestBid.Amount
					&& _notificationService != null)
				{
					var prevBidderId = previousHighestBid.BidderId;
					var itemTitle = auction.Item?.Title ?? $"Auction #{auctionId}";
					var message = $"Bạn đã bị vượt giá ở phiên \"{itemTitle}\"";

					await _notificationService.CreateNotificationAsync(new CreateNotificationDto
					{
						UserId = prevBidderId,
						Type = "bid_outbid",
						Message = message,
						Link = $"/auction/{auctionId}"
					});
				}
			}
			catch
			{
				// Không block flow đặt giá nếu gửi thông báo thất bại
			}

			// Broadcast SignalR nếu có notification service (cho cả manual và auto bid)
			// Broadcast ngay lập tức để tất cả clients nhận được update real-time
			if (_bidNotificationService != null)
			{
				try
				{
					await _bidNotificationService.BroadcastBidPlacedAsync(auctionId, result);
				}
				catch
				{
					// Silently fail - broadcast không thành công không ảnh hưởng đến bid
				}
			}

			// Xử lý auto bid sau khi đặt giá thành công (cho cả manual và auto bid)
			// CRITICAL: Cần trigger auto bid processing cả khi auto bid đặt giá
			// để các auto bid khác có thể tiếp tục đấu giá với nhau
			// Logic trong ProcessAutoBidsAfterBidAsync đã có check để tránh infinite loop:
			// - Skip người vừa đặt giá (currentBidderId)
			// - Reload auction mỗi lần để lấy giá mới nhất
			// - Chỉ đặt giá nếu giá mới > giá hiện tại
			_ = Task.Run(async () =>
			{
				try
				{
					using var scope = _serviceScopeFactory.CreateScope();
					var autoBidService = scope.ServiceProvider.GetRequiredService<IAutoBidService>();
					await autoBidService.ProcessAutoBidsAfterBidAsync(auctionId, bidderId, amount);
				}
				catch (Exception ex)
				{
					// Log error để debug
					System.Diagnostics.Debug.WriteLine($"Auto bid processing error: {ex.Message}");
					System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
				}
			});

			return result;
		}

	public async Task<IReadOnlyList<BidDto>> GetRecentBidsAsync(int auctionId, int limit)
	{
		// Ưu tiên đọc từ Redis (cache nóng) để tránh query SQL liên tục.
		if (_redis is not null)
		{
			try
			{
				var db = _redis.GetDatabase();
				var entries = await db.SortedSetRangeByRankAsync(BidsKey(auctionId), -limit, -1, StackExchange.Redis.Order.Ascending);
				if (entries?.Length > 0)
				{
					var result = new List<BidDto>();
					foreach (var entry in entries)
					{
						if (entry.IsNullOrEmpty) continue;
						try
						{
							var bid = JsonSerializer.Deserialize<BidDto>(entry!);
							if (bid != null)
							{
								if (string.IsNullOrWhiteSpace(bid.BidderName))
								{
									bid.BidderName = $"User #{bid.BidderId}";
								}
								result.Add(bid);
							}
						}
						catch
						{
							// Skip invalid entries
							continue;
						}
					}
					if (result.Count > 0)
					{
						return result;
					}
				}
			}
			catch
			{
				// Redis error, fallback to SQL
			}
		}
		// Cache trống hoặc Redis không sẵn sàng => fallback đọc trực tiếp từ SQL.
		var list = await _bidRepository.GetRecentByAuctionAsync(auctionId, limit);
		return list.Select(b => new BidDto
		{
			BidderId = b.BidderId,
			BidderName = b.Bidder?.FullName ?? $"User #{b.BidderId}",
			Amount = b.Amount,
			BidTime = b.BidTime ?? DateTime.Now
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
        public async Task<PaginatedResultB<BiddingHistoryDto>> GetBiddingHistoryAsync(int bidderId, int page, int pageSize)
        {
            try
            {
                var skip = (page - 1) * pageSize;

                // ✅ THAY ĐỔI: Lấy theo AUCTION thay vì theo BID
                // Mỗi auction chỉ xuất hiện 1 lần với bid cao nhất của user
                var auctionData = await _ctx.Auctions
                    .Where(a => a.Status == "completed"
                               && a.Bids.Any(b => b.BidderId == bidderId))
                    .OrderByDescending(a => a.EndTime)
                    .Skip(skip)
                    .Take(pageSize)
                    .Select(a => new
                    {
                        Auction = a,
                        // Lấy bid cao nhất của user trong auction này
                        HighestBid = a.Bids
                            .Where(b => b.BidderId == bidderId)
                            .OrderByDescending(b => b.Amount)
                            .ThenByDescending(b => b.BidTime)
                            .FirstOrDefault(),
                        // Lấy bid cao nhất của TẤT CẢ bidders trong auction (để xác định winner nếu WinnerId chưa set)
                        HighestBidInAuction = a.Bids
                            .OrderByDescending(b => b.Amount)
                            .ThenByDescending(b => b.BidTime)
                            .FirstOrDefault(),
                        AuctionId = a.Id,
                        AuctionStatus = a.Status,
                        AuctionWinnerId = a.WinnerId,
                        AuctionCurrentBid = a.CurrentBid,
                        AuctionEndTime = a.EndTime,
                        ItemTitle = a.Item.Title,
                        ItemImages = a.Item.Images,
                        CategoryName = a.Item.Category.Name
                    })
                    .AsNoTracking()
                    .ToListAsync();

                // ✅ Count theo AUCTION, không phải BID
                var totalCount = await _ctx.Auctions
                    .Where(a => a.Status == "completed"
                               && a.Bids.Any(b => b.BidderId == bidderId))
                    .CountAsync();

                var historyList = new List<BiddingHistoryDto>();

                foreach (var item in auctionData)
                {
                    // Bỏ qua nếu không có bid (edge case)
                    if (item.HighestBid == null) continue;

                    // ✅ Logic status đã được sửa
                    string status;
                    if (string.Equals(item.AuctionStatus, "completed", StringComparison.OrdinalIgnoreCase))
                    {
                        // CRITICAL: Ưu tiên kiểm tra WinnerId trước (nếu đã được set)
                        if (item.AuctionWinnerId.HasValue)
                        {
                            // Nếu WinnerId đã được set, chỉ cần kiểm tra xem có match với bidderId không
                            status = item.AuctionWinnerId.Value == bidderId ? "won" : "lost";
                        }
                        else if (item.HighestBidInAuction != null)
                        {
                            // Fallback: Nếu WinnerId chưa được set, kiểm tra xem user có phải là người đặt giá cao nhất không
                            // Sử dụng HighestBidInAuction đã được load trong query
                            status = item.HighestBidInAuction.BidderId == bidderId ? "won" : "lost";
                        }
                        else
                        {
                            // Không có bids nào, không ai thắng
                            status = "lost";
                        }
                    }
                    else if (string.Equals(item.AuctionStatus, "active", StringComparison.OrdinalIgnoreCase))
                    {
                        status = item.AuctionCurrentBid.HasValue
                                 && item.HighestBid.Amount >= item.AuctionCurrentBid.Value
                            ? "leading"
                            : "outbid";
                    }
                    else
                    {
                        status = "lost";
                    }

                    historyList.Add(new BiddingHistoryDto
                    {
                        BidId = item.HighestBid.Id,
                        AuctionId = item.AuctionId,
                        ItemTitle = item.ItemTitle ?? "Unknown Item",
                        ItemImages = item.ItemImages,
                        CategoryName = item.CategoryName,
                        YourBid = item.HighestBid.Amount,
                        BidTime = item.HighestBid.BidTime ?? DateTime.Now,
                        Status = status,
                        CurrentBid = item.AuctionCurrentBid,
                        EndTime = item.AuctionEndTime,
                        AuctionStatus = item.AuctionStatus,
                        IsAutoBid = item.HighestBid.IsAutoBid ?? false
                    });
                }

                return new PaginatedResultB<BiddingHistoryDto>
                {
                    Data = historyList,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize
                };
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error retrieving bidding history: {ex.Message}", ex);
            }
        }

        public async Task<IReadOnlyList<int>> GetDistinctBidderIdsByAuctionAsync(int auctionId)
        {
            return await _bidRepository.GetDistinctBidderIdsByAuctionAsync(auctionId);
        }

		/// <summary>
		/// Đặt TTL (Time To Live) cho các Redis keys của auction sau khi auction kết thúc.
		/// Sau khi TTL hết hạn, Redis sẽ tự động xóa các keys này để giải phóng bộ nhớ.
		/// </summary>
		/// <param name="auctionId">ID của auction đã kết thúc</param>
		/// <param name="expirationMinutes">Số phút trước khi keys bị xóa (mặc định: 30 phút)</param>
		public async Task SetAuctionCacheExpirationAsync(int auctionId, int expirationMinutes = 30)
		{
			if (_redis is null)
			{
				// Redis không sẵn sàng, không cần làm gì
				return;
			}

			try
			{
				var db = _redis.GetDatabase();
				var expiration = TimeSpan.FromMinutes(expirationMinutes);

				// Set TTL cho cả hai keys: bids và highest
				var bidsKey = BidsKey(auctionId);
				var highestKey = HighestKey(auctionId);

				// Kiểm tra xem keys có tồn tại không trước khi set TTL
				var bidsExists = await db.KeyExistsAsync(bidsKey);
				var highestExists = await db.KeyExistsAsync(highestKey);

				if (bidsExists)
				{
					await db.KeyExpireAsync(bidsKey, expiration);
				}

				if (highestExists)
				{
					await db.KeyExpireAsync(highestKey, expiration);
				}
			}
			catch (Exception ex)
			{
				// Log error nhưng không throw để không ảnh hưởng đến flow chính
				// Redis error không nên block việc finalize auction
				System.Diagnostics.Debug.WriteLine($"Error setting Redis TTL for auction {auctionId}: {ex.Message}");
			}
		}
    }
}


