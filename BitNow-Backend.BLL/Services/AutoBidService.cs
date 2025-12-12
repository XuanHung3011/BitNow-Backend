using BitNow_Backend.BLL.IServices;
using BitNow_Backend.DAL;
using BitNow_Backend.DAL.DTOs;
using BitNow_Backend.DAL.IRepositories;
using BitNow_Backend.DAL.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BitNow_Backend.BLL.Services
{
	/// <summary>
	/// Service xử lý auto bid với thuật toán bước nhảy giá động dựa trên giá hiện tại
	/// </summary>
	public class AutoBidService : IAutoBidService
	{
		private readonly BidNowDbContext _ctx;
		private readonly IAutoBidRepository _autoBidRepository;
		private readonly IServiceScopeFactory _serviceScopeFactory;

		public AutoBidService(
			BidNowDbContext ctx,
			IAutoBidRepository autoBidRepository,
			IServiceProvider serviceProvider
		)
		{
			_ctx = ctx;
			_autoBidRepository = autoBidRepository;
			_serviceScopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
		}

		/// <summary>
		/// Tính bước nhảy giá dựa trên giá hiện tại (tham khảo từ bảng bid increment)
		/// </summary>
		public decimal CalculateBidIncrement(decimal currentPrice)
		{
			if (currentPrice < 25000)
			{
				// 0 - 24,999 VND: 1,250 VND (tương đương $0.05)
				return 1250;
			}
			else if (currentPrice < 125000)
			{
				// 25,000 - 124,999 VND: 6,250 VND (tương đương $0.25)
				return 6250;
			}
			else if (currentPrice < 625000)
			{
				// 125,000 - 624,999 VND: 12,500 VND (tương đương $0.50)
				return 12500;
			}
			else if (currentPrice < 2500000)
			{
				// 625,000 - 2,499,999 VND: 25,000 VND (tương đương $1.00)
				return 25000;
			}
			else if (currentPrice < 6250000)
			{
				// 2,500,000 - 6,249,999 VND: 62,500 VND (tương đương $2.50)
				return 62500;
			}
			else if (currentPrice < 12500000)
			{
				// 6,250,000 - 12,499,999 VND: 125,000 VND (tương đương $5.00)
				return 125000;
			}
			else if (currentPrice < 25000000)
			{
				// 12,500,000 - 24,999,999 VND: 250,000 VND (tương đương $10.00)
				return 250000;
			}
			else if (currentPrice < 62500000)
			{
				// 25,000,000 - 62,499,999 VND: 625,000 VND (tương đương $25.00)
				return 625000;
			}
			else if (currentPrice < 125000000)
			{
				// 62,500,000 - 124,999,999 VND: 1,250,000 VND (tương đương $50.00)
				return 1250000;
			}
			else
			{
				// 125,000,000+ VND: 2,500,000 VND (tương đương $100.00)
				return 2500000;
			}
		}

		public async Task<AutoBidDto> CreateOrUpdateAutoBidAsync(int auctionId, int userId, decimal maxAmount)
		{
			var auction = await _ctx.Auctions.FindAsync(auctionId);
			if (auction == null) throw new InvalidOperationException("Auction not found");
			if (auction.Status != "active") throw new InvalidOperationException("Auction not active");

			var currentBid = auction.CurrentBid ?? auction.StartingBid;
			if (maxAmount <= currentBid) throw new InvalidOperationException("Max amount must be higher than current bid");

			var autoBid = new AutoBid
			{
				AuctionId = auctionId,
				UserId = userId,
				MaxAmount = maxAmount,
				IsActive = true,
				CreatedAt = DateTime.Now
			};

			var result = await _autoBidRepository.CreateOrUpdateAsync(autoBid);
			return new AutoBidDto
			{
				Id = result.Id,
				AuctionId = result.AuctionId,
				UserId = result.UserId,
				MaxAmount = result.MaxAmount,
				IsActive = result.IsActive ?? false,
				CreatedAt = result.CreatedAt
			};
		}

		public async Task<bool> DeactivateAutoBidAsync(int auctionId, int userId)
		{
			var autoBid = await _autoBidRepository.GetByAuctionAndUserAsync(auctionId, userId);
			if (autoBid == null) return false;
			return await _autoBidRepository.DeactivateAsync(autoBid.Id);
		}

		public async Task<AutoBidDto?> GetAutoBidAsync(int auctionId, int userId)
		{
			var autoBid = await _autoBidRepository.GetByAuctionAndUserAsync(auctionId, userId);
			if (autoBid == null || !autoBid.IsActive.HasValue || !autoBid.IsActive.Value) return null;
			return new AutoBidDto
			{
				Id = autoBid.Id,
				AuctionId = autoBid.AuctionId,
				UserId = autoBid.UserId,
				MaxAmount = autoBid.MaxAmount,
				IsActive = autoBid.IsActive ?? false,
				CreatedAt = autoBid.CreatedAt
			};
		}

		/// <summary>
		/// Xử lý auto bid sau khi có bid mới: kiểm tra các auto bid active và tự động đặt giá nếu cần
		/// CRITICAL: Method này sẽ được gọi cả khi manual bid và auto bid đặt giá
		/// Logic đảm bảo các auto bid có thể tiếp tục đấu giá với nhau cho đến khi đạt maxAmount
		/// </summary>
		public async Task ProcessAutoBidsAfterBidAsync(int auctionId, int currentBidderId, decimal currentBid)
		{
			var activeAutoBids = await _autoBidRepository.GetActiveByAuctionAsync(auctionId);
			if (activeAutoBids.Count == 0) return; // Không có auto bid nào active

			var auction = await _ctx.Auctions.FindAsync(auctionId);
			if (auction == null || auction.Status != "active") return;

			// CRITICAL: Sắp xếp auto bids theo maxAmount (từ cao xuống thấp) để ưu tiên người có maxAmount cao hơn
			// Điều này giúp đảm bảo auto bid với maxAmount cao hơn sẽ đấu giá trước
			var sortedAutoBids = activeAutoBids.OrderByDescending(ab => ab.MaxAmount).ToList();
			var deactivatedAutoBidIds = new HashSet<int>(); // Track các auto bid đã bị deactivate

			// CRITICAL: Track người đã đặt giá trong mỗi iteration để tránh một user đặt giá nhiều lần liên tiếp
			// Mỗi user chỉ được đặt giá một lần trong một chu kỳ process
			// Chỉ tiếp tục khi có user KHÁC đặt giá (trigger lại ProcessAutoBidsAfterBidAsync)
			var usersWhoBidInThisCycle = new HashSet<int> { currentBidderId }; // Bắt đầu với người trigger
			int maxIterations = sortedAutoBids.Count * 2; // Giảm iterations để tránh một user đặt nhiều lần
			int iterationCount = 0;
			bool anyBidPlaced = true;
			int lastBidderId = currentBidderId; // Track người đặt giá cuối cùng

			// Tiếp tục process cho đến khi không còn auto bid nào có thể đặt giá
			while (anyBidPlaced && iterationCount < maxIterations)
			{
				anyBidPlaced = false;
				iterationCount++;
				int newBidderId = -1; // Track người đặt giá trong iteration này

				foreach (var autoBid in sortedAutoBids)
				{
					// Bỏ qua nếu đã bị deactivate
					if (deactivatedAutoBidIds.Contains(autoBid.Id)) continue;

					// CRITICAL: Bỏ qua nếu user này đã đặt giá trong chu kỳ này
					// Chỉ cho phép một user đặt giá một lần trong một chu kỳ process
					// Để tiếp tục đấu giá, cần có user KHÁC trigger lại ProcessAutoBidsAfterBidAsync
					if (usersWhoBidInThisCycle.Contains(autoBid.UserId)) continue;

					// QUAN TRỌNG: Fetch lại giá mới nhất từ database trong mỗi iteration
					// để đảm bảo luôn dùng giá mới nhất (có thể đã thay đổi bởi auto bid trước đó)
					await _ctx.Entry(auction).ReloadAsync();
					var latestCurrentBid = auction.CurrentBid ?? auction.StartingBid;

					// Kiểm tra xem có thể đặt giá cao hơn không
					var increment = CalculateBidIncrement(latestCurrentBid);
					var nextBid = latestCurrentBid + increment;

					// Nếu giá tiếp theo vẫn trong giới hạn maxAmount và chưa vượt quá
					if (nextBid <= autoBid.MaxAmount && nextBid > latestCurrentBid)
					{
						try
						{
							// Resolve IBidService từ service provider để tránh circular dependency
							// Tạo scope mới để tránh vấn đề với DbContext
							// IBidService sẽ tự động broadcast SignalR qua IBidNotificationService
							using var scope = _serviceScopeFactory.CreateScope();
							var bidService = scope.ServiceProvider.GetRequiredService<IBidService>();
							// Tự động đặt giá với IsAutoBid = true
							// Broadcast SignalR sẽ được xử lý tự động trong BidService.PlaceBidAsync
							var bidResult = await bidService.PlaceBidAsync(auctionId, autoBid.UserId, nextBid, isAutoBid: true);
							
							// Log để debug
							System.Diagnostics.Debug.WriteLine($"Auto bid placed: Auction {auctionId}, User {autoBid.UserId}, Amount {nextBid}, New Current: {bidResult.CurrentBid}, Iteration: {iterationCount}");
							
							// Đánh dấu user này đã đặt giá trong chu kỳ này
							usersWhoBidInThisCycle.Add(autoBid.UserId);
							newBidderId = autoBid.UserId;
							lastBidderId = autoBid.UserId;
							anyBidPlaced = true; // Đánh dấu có bid được đặt trong iteration này
							
							// CRITICAL: Break sau khi một user đặt giá thành công
							// Chỉ cho phép một user đặt giá một lần trong mỗi iteration
							// Để tiếp tục, cần có user khác trigger lại ProcessAutoBidsAfterBidAsync
							// Điều này đảm bảo các auto bid đấu giá với nhau, không phải một mình
							break;
						}
						catch (Exception ex)
						{
							// Log error để debug
							System.Diagnostics.Debug.WriteLine($"Auto bid place bid error: {ex.Message}");
							// Nếu đặt giá thất bại (có thể do đã bị người khác vượt), bỏ qua
							continue;
						}
					}
					else if (nextBid > autoBid.MaxAmount)
					{
						// Vượt quá max amount, deactivate auto bid
						await _autoBidRepository.DeactivateAsync(autoBid.Id);
						deactivatedAutoBidIds.Add(autoBid.Id); // Đánh dấu đã deactivate
					}
				}

				// CRITICAL: Nếu không có user nào đặt giá trong iteration này, dừng lại
				// Điều này đảm bảo không có user nào đặt giá một mình nhiều lần
				if (!anyBidPlaced)
				{
					System.Diagnostics.Debug.WriteLine($"No more auto bids can place bid in iteration {iterationCount} for auction {auctionId}");
					break;
				}

				// Delay nhỏ để tránh race condition và đảm bảo database được cập nhật
				await Task.Delay(150);
			}

			if (iterationCount >= maxIterations)
			{
				System.Diagnostics.Debug.WriteLine($"Auto bid processing reached max iterations ({maxIterations}) for auction {auctionId}");
			}
		}
	}
}

