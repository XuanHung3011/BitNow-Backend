using BitNow_Backend.BLL.IServices;
using BitNow_Backend.DAL;
using BitNow_Backend.DAL.DTOs;
using BitNow_Backend.DAL.IRepositories;
using BitNow_Backend.DAL.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace BitNow_Backend.BLL.Services
{
	public class AuctionService : IAuctionService
	{
        private readonly IAuctionRepository _auctionRepository;
        private readonly IItemRepository _itemRepository;
        private readonly IBidRepository _bidRepository;
        private readonly BidNowDbContext _dbContext;
        private readonly ILogger<AuctionService> _logger;
        private readonly IBidService? _bidService;
        // Cho phép cả trạng thái tạm dừng (paused) và hủy (cancelled)
        private static readonly HashSet<string> AllowedStatuses = new(StringComparer.OrdinalIgnoreCase) 
        { 
            "draft", 
            "active", 
            "scheduled", 
            "completed", 
            "paused",
            "cancelled"
        };

        public AuctionService(
            IAuctionRepository auctionRepository,
            IItemRepository itemRepository,
            IBidRepository bidRepository,
            BidNowDbContext dbContext,
            ILogger<AuctionService> logger,
            IBidService? bidService = null)
        {
            _auctionRepository = auctionRepository;
            _itemRepository = itemRepository;
            _bidRepository = bidRepository;
            _dbContext = dbContext;
            _logger = logger;
            _bidService = bidService;
        }

        public async Task<AuctionDetailDto?> GetDetailAsync(int id)
		{
			// Update status if needed before returning (lazy update)
			await _auctionRepository.UpdateAuctionStatusIfNeededAsync(id);
			
			var a = await _auctionRepository.GetByIdAsync(id);
			if (a == null) return null;
			return new AuctionDetailDto
			{
				Id = a.Id,
				ItemId = a.ItemId,
				ItemTitle = a.Item.Title,
				ItemDescription = a.Item.Description,
				ItemSpecifics = a.Item.ItemSpecifics,
				ItemImages = a.Item.Images,
				CategoryId = a.Item.CategoryId,
                CategoryName = a.Item.Category?.Name,
                SellerId = a.SellerId,
                SellerName = a.Seller?.FullName,
                SellerTotalRatings = a.Seller?.TotalRatings,
                StartingBid = a.StartingBid,
				CurrentBid = a.CurrentBid,
				BuyNowPrice = a.BuyNowPrice,
				StartTime = a.StartTime,
				EndTime = a.EndTime,
				Status = a.Status,
				BidCount = a.BidCount,
				PausedAt = a.PausedAt,
                WinnerId = a.WinnerId,
                WinnerName = a.Winner?.FullName
			};
		}

        public async Task<PaginatedResult<AuctionListItemDto>> GetAuctionsWithFilterAsync(AuctionFilterDto filter)
		{
			var (auctions, totalCount) = await _auctionRepository.GetAuctionsWithFilterAsync(filter);
			var now = DateTime.Now; // Use local time (Vietnam time) - matches database storage

			var items = auctions.Select(a =>
			{
				// Determine display status based on actual time, not just Status field
				// Priority: cancelled > draft > scheduled > active > completed
				string displayStatus;
                if (a.Status != null && a.Status.Equals("paused", StringComparison.OrdinalIgnoreCase))
                {
                    displayStatus = "paused";
                }
                else if (a.Status != null && a.Status.Equals("cancelled", StringComparison.OrdinalIgnoreCase))
                {
                    displayStatus = "cancelled";
				}
				// 2. Draft: status = "draft"
				else if (a.Status != null && a.Status.ToLower() == "draft")
				{
					displayStatus = "draft";
				}
				// 3. Scheduled: Chưa đến giờ bắt đầu (StartTime > now)
				else if (a.StartTime > now)
				{
					displayStatus = "scheduled";
				}
				// 4. Active: Đã bắt đầu và chưa kết thúc (StartTime <= now && EndTime > now)
				else if (a.StartTime <= now && a.EndTime > now)
				{
					displayStatus = "active";
				}
				// 5. Completed: Đã kết thúc (EndTime <= now)
				else if (a.EndTime <= now)
				{
					displayStatus = "completed";
				}
				// Fallback: Use status field if time logic doesn't match
				else
				{
					displayStatus = a.Status?.ToLower() ?? "unknown";
				}

				// Parse images from item
				var itemImages = a.Item?.Images;
				var firstImage = "";
				if (!string.IsNullOrEmpty(itemImages))
				{
					try
					{
						var imageList = System.Text.Json.JsonSerializer.Deserialize<List<string>>(itemImages);
						firstImage = imageList?.FirstOrDefault() ?? "";
					}
					catch
					{
						// If not JSON, try comma-separated
						firstImage = itemImages.Split(',').FirstOrDefault()?.Trim() ?? "";
					}
				}

				return new AuctionListItemDto
				{
					Id = a.Id,
					ItemTitle = a.Item?.Title ?? "",
					ItemImages = itemImages, // Include full images string for frontend to parse
					SellerName = a.Seller?.FullName,
					CategoryName = a.Item?.Category?.Name,
					StartingBid = a.StartingBid,
					CurrentBid = a.CurrentBid,
                    StartTime = a.StartTime,
					EndTime = a.EndTime,
					Status = a.Status ?? "",
					DisplayStatus = displayStatus,
					BidCount = a.BidCount ?? 0,
					PausedAt = a.PausedAt
				};
			}).ToList();

			return new PaginatedResult<AuctionListItemDto>
			{
				Data = items,
				TotalCount = totalCount,
				Page = filter.Page,
				PageSize = filter.PageSize
			};
		}

        public async Task<bool> UpdateStatusAsync(int id, string status)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                throw new ArgumentException("Status is required", nameof(status));
            }

            if (!AllowedStatuses.Contains(status))
            {
                throw new ArgumentException($"Status must be one of: {string.Join(", ", AllowedStatuses)}");
            }

            return await _auctionRepository.UpdateStatusAsync(id, status);
        }

        public async Task<bool> ResumeAuctionAsync(int id)
        {
            return await _auctionRepository.ResumeAuctionAsync(id);
        }

        public async Task<AuctionResponseDto?> CreateAuctionAsync(CreateAuctionDto dto)
        {
            // Validate item exists
            var item = await _itemRepository.GetByIdAsync(dto.ItemId);
            if (item == null)
            {
                throw new ArgumentException("Item not found");
            }

            // Allow creating auction for items with status "pending" or "approved"
            // Rejected items cannot have auctions
            if (item.Status == "rejected")
            {
                throw new InvalidOperationException("Cannot create auction for a rejected item");
            }

            // Validate seller owns the item
            if (item.SellerId != dto.SellerId)
            {
                throw new UnauthorizedAccessException("You can only create auctions for your own items");
            }

            // Check if item already has an active auction
            // Load Auctions navigation property if not loaded
            if (item.Auctions == null)
            {
                // Reload item with Auctions included
                item = await _itemRepository.GetByIdAsync(dto.ItemId);
            }

            if (item.Auctions != null && item.Auctions.Any(a => a.Status == "active" || a.Status == "draft" || a.Status == "scheduled"))
            {
                throw new InvalidOperationException("Item already has an active, draft, or scheduled auction");
            }

            // Convert UTC time from frontend to local time (Vietnam time UTC+7)
            // Frontend sends UTC time in ISO format, we need to convert to local time
            // Vietnam timezone: UTC+7
            DateTime startTimeLocal;
            DateTime endTimeLocal;

            try
            {
                // Try to get Vietnam timezone
                TimeZoneInfo vietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"); // Vietnam timezone on Windows
                
                // Check if dto.StartTime is UTC (Kind = Utc) or Unspecified
                if (dto.StartTime.Kind == DateTimeKind.Utc)
                {
                    // Convert from UTC to Vietnam local time
                    startTimeLocal = TimeZoneInfo.ConvertTimeFromUtc(dto.StartTime, vietnamTimeZone);
                    endTimeLocal = TimeZoneInfo.ConvertTimeFromUtc(dto.EndTime, vietnamTimeZone);
                }
                else if (dto.StartTime.Kind == DateTimeKind.Unspecified)
                {
                    // Assume it's UTC if unspecified (from JSON deserialization of ISO string)
                    // JSON deserializer treats ISO strings with 'Z' as UTC but sets Kind to Unspecified
                    startTimeLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(dto.StartTime, DateTimeKind.Utc), vietnamTimeZone);
                    endTimeLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(dto.EndTime, DateTimeKind.Utc), vietnamTimeZone);
                }
                else
                {
                    // Already local time, use as is
                    startTimeLocal = dto.StartTime;
                    endTimeLocal = dto.EndTime;
                }
            }
            catch (TimeZoneNotFoundException)
            {
                // Fallback: Use UTC+7 offset directly if timezone not found
                // This works on both Windows and Linux
                const int vietnamOffsetHours = 7;
                TimeSpan vietnamOffset = TimeSpan.FromHours(vietnamOffsetHours);
                
                if (dto.StartTime.Kind == DateTimeKind.Utc)
                {
                    startTimeLocal = dto.StartTime.Add(vietnamOffset);
                    endTimeLocal = dto.EndTime.Add(vietnamOffset);
                }
                else if (dto.StartTime.Kind == DateTimeKind.Unspecified)
                {
                    // Assume UTC and add offset
                    startTimeLocal = DateTime.SpecifyKind(dto.StartTime, DateTimeKind.Utc).Add(vietnamOffset);
                    endTimeLocal = DateTime.SpecifyKind(dto.EndTime, DateTimeKind.Utc).Add(vietnamOffset);
                }
                else
                {
                    startTimeLocal = dto.StartTime;
                    endTimeLocal = dto.EndTime;
                }
            }

            // Validate dates using local time (Vietnam time)
            var nowLocal = DateTime.Now; // This is already local time (Vietnam time)

            if (startTimeLocal >= endTimeLocal)
            {
                throw new ArgumentException("Start time must be before end time");
            }

            if (startTimeLocal < nowLocal)
            {
                throw new ArgumentException("Start time cannot be in the past");
            }

            // Determine status based on start time
            // If start time is in the future, set status to "scheduled"
            // Otherwise, set status to "active"
            string auctionStatus = startTimeLocal > nowLocal ? "scheduled" : "active";

            // Create auction with appropriate status
            // Only set foreign key IDs, not navigation properties
            // Store times as local time (Vietnam time) - same as DateTime.Now
            var auction = new Auction
            {
                ItemId = dto.ItemId,
                SellerId = dto.SellerId,
                StartingBid = dto.StartingBid,
                BuyNowPrice = dto.BuyNowPrice,
                StartTime = startTimeLocal, // Store as local time (Vietnam time)
                EndTime = endTimeLocal, // Store as local time (Vietnam time)
                Status = auctionStatus, // Set to "scheduled" if start time is in future, "active" otherwise
                BidCount = 0,
                CurrentBid = null,
                CreatedAt = DateTime.Now, // Local time (Vietnam time)
                WinnerId = null
            };

            var createdAuction = await _auctionRepository.CreateAsync(auction);

            // Update item status to "archived" so it won't appear in "approved items" list anymore
            // Item has been used for auction, so it should not be available for creating another auction
            await _itemRepository.UpdateItemStatusAsync(dto.ItemId, "archived");

            return new AuctionResponseDto
            {
                Id = createdAuction.Id,
                ItemId = createdAuction.ItemId,
                SellerId = createdAuction.SellerId,
                StartingBid = createdAuction.StartingBid,
                CurrentBid = createdAuction.CurrentBid,
                BuyNowPrice = createdAuction.BuyNowPrice,
                StartTime = createdAuction.StartTime,
                EndTime = createdAuction.EndTime,
                Status = createdAuction.Status,
                BidCount = createdAuction.BidCount,
                CreatedAt = createdAuction.CreatedAt
            };
        }
        public async Task<PaginatedResult<BuyerActiveBidDto>> GetActiveBidsByBuyerAsync(int bidderId, int page = 1, int pageSize = 10)
        {
            var (auctions, totalCount) = await _auctionRepository.GetAuctionsByBidderAsync(bidderId, page, pageSize);

            var items = auctions.Select(a =>
            {
                // Get user's bids for this auction
                var userBids = a.Bids?.Where(b => b.BidderId == bidderId).ToList() ?? new List<Bid>();
                var userHighestBid = userBids.Any() ? userBids.Max(b => b.Amount) : 0;

                // Check if user is leading
                var currentBid = a.CurrentBid ?? a.StartingBid;
                var isLeading = userHighestBid >= currentBid;

                return new BuyerActiveBidDto
                {
                    AuctionId = a.Id,
                    ItemTitle = a.Item?.Title ?? "",
                    ItemImages = a.Item?.Images,
                    CategoryName = a.Item?.Category?.Name,
                    CurrentBid = currentBid,
                    YourHighestBid = userHighestBid,
                    IsLeading = isLeading,
                    EndTime = a.EndTime,
                    TotalBids = a.BidCount ?? 0,
                    YourBidCount = userBids.Count
                };
            }).ToList();

            return new PaginatedResult<BuyerActiveBidDto>
            {
                Data = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }
        public async Task<PaginatedResult<BuyerWonAuctionDto>> GetWonAuctionsByBuyerAsync(int bidderId, int page = 1, int pageSize = 10)
        {
            var (auctions, totalCount) = await _auctionRepository.GetWonAuctionsByBidderAsync(bidderId, page, pageSize);

            // Get all orders for these auctions in one query
            var auctionIds = auctions.Select(a => a.Id).ToList();
            var orders = await _dbContext.Orders
                .Include(o => o.Payment)
                .Where(o => o.BuyerId == bidderId && auctionIds.Contains(o.AuctionId))
                .ToListAsync();

            var items = auctions.Select(a =>
            {
                // Get user's highest bid (which should be the winning bid)
                var userBids = a.Bids?.Where(b => b.BidderId == bidderId).ToList() ?? new List<Bid>();
                var finalBid = userBids.Any() ? userBids.Max(b => b.Amount) : (a.CurrentBid ?? a.StartingBid);

                // Check if user has rated (you'll need to implement this based on your Rating system)
                // For now, defaulting to false
                var hasRated = false; // TODO: Check if rating exists for this auction and buyer

                // Find order for this auction
                var order = orders.FirstOrDefault(o => o.AuctionId == a.Id);
                var payment = order?.Payment;

                return new BuyerWonAuctionDto
                {
                    AuctionId = a.Id,
                    ItemTitle = a.Item?.Title ?? "",
                    ItemImages = a.Item?.Images,
                    CategoryName = a.Item?.Category?.Name,
                    FinalBid = finalBid,
                    WonDate = a.EndTime, // Use EndTime as WonDate
                    EndTime = a.EndTime,
                    Status = a.Status ?? "completed",
                    SellerName = a.Seller?.FullName,
                    SellerId = a.SellerId,
                    HasRated = hasRated,
                    // Order and Payment information
                    OrderId = order?.Id,
                    OrderStatus = order?.OrderStatus,
                    PaymentStatus = payment?.PaymentStatus,
                    PaidAt = payment?.PaidAt,
                    HasOrder = order != null,
                    HasPayment = payment != null
                };
            }).ToList();

            return new PaginatedResult<BuyerWonAuctionDto>
            {
                Data = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<List<SellerAuctionDto>> GetAuctionsBySellerAsync(int sellerId)
        {
            var auctions = await _auctionRepository.GetAuctionsBySellerAsync(sellerId);
            var now = DateTime.Now;

            var result = auctions.Select(a =>
            {
                // Determine display status based on time, not just status field
                // Priority: draft > paused > cancelled > scheduled > active > completed
                string displayStatus;
                
                // 1. Draft: status = "draft"
                if (a.Status != null && a.Status.ToLower() == "draft")
                {
                    displayStatus = "draft";
                }
                // 2. Paused: status = "paused"
                else if (a.Status != null && a.Status.Equals("paused", StringComparison.OrdinalIgnoreCase))
                {
                    displayStatus = "paused";
                }
                // 3. Cancelled: status = "cancelled"
                else if (a.Status != null && a.Status.Equals("cancelled", StringComparison.OrdinalIgnoreCase))
                {
                    displayStatus = "cancelled";
                }
                // 4. Scheduled: Chưa đến giờ bắt đầu (StartTime > now)
                else if (a.StartTime > now)
                {
                    displayStatus = "scheduled";
                }
                // 5. Active: Đã bắt đầu và chưa kết thúc (StartTime <= now && EndTime > now)
                else if (a.StartTime <= now && a.EndTime > now)
                {
                    displayStatus = "active";
                }
                // 6. Completed: Đã kết thúc (EndTime <= now)
                else if (a.EndTime <= now)
                {
                    displayStatus = "completed";
                }
                // Fallback: Use status field if time logic doesn't match
                else
                {
                    displayStatus = a.Status?.ToLower() ?? "unknown";
                }

                // Check if seller has rated the buyer (for completed auctions)
                var hasRated = false;
                if (displayStatus == "completed" && a.WinnerId != null)
                {
                    // TODO: Check if rating exists for this auction where raterId == sellerId and ratedId == winnerId
                    // For now, defaulting to false
                }

                // Parse images
                var images = a.Item?.Images;
                var firstImage = "";
                if (!string.IsNullOrEmpty(images))
                {
                    try
                    {
                        var imageList = System.Text.Json.JsonSerializer.Deserialize<List<string>>(images);
                        firstImage = imageList?.FirstOrDefault() ?? "";
                    }
                    catch
                    {
                        // If not JSON, try comma-separated
                        firstImage = images.Split(',').FirstOrDefault()?.Trim() ?? "";
                    }
                }

                return new SellerAuctionDto
                {
                    Id = a.Id,
                    ItemId = a.ItemId,
                    ItemTitle = a.Item?.Title ?? "",
                    ItemImages = firstImage,
                    CategoryName = a.Item?.Category?.Name,
                    StartingBid = a.StartingBid,
                    CurrentBid = a.CurrentBid,
                    BuyNowPrice = a.BuyNowPrice,
                    BidCount = a.BidCount ?? 0,
                    StartTime = a.StartTime,
                    EndTime = a.EndTime,
                    Status = a.Status ?? "",
                    DisplayStatus = displayStatus,
                    WinnerId = a.WinnerId,
                    WinnerName = a.Winner?.FullName,
                    HasRated = hasRated
                };
            }).ToList();

            return result;
        }

        public async Task<IEnumerable<ItemResponseDto>> GetItemsByAuctionIdsAsync(IEnumerable<int> auctionIds)
        {
            var auctions = await _auctionRepository.GetAuctionsByIdsAsync(auctionIds);

            return auctions.Select(a => new ItemResponseDto
            {
                Id = a.Item.Id,
                Title = a.Item.Title,
                Description = a.Item.Description,
                BasePrice = a.Item.BasePrice,
                Condition = a.Item.Condition,
                Images = a.Item.Images,
                Location = a.Item.Location,
                Status = a.Item.Status,
                CreatedAt = a.Item.CreatedAt,

                // Category Info
                CategoryId = a.Item.CategoryId,
                CategoryName = a.Item.Category?.Name,
                CategorySlug = a.Item.Category?.Slug,
                CategoryIcon = a.Item.Category?.Icon,

                // Seller Info
                SellerId = a.Item.SellerId,
                SellerName = a.Seller?.FullName,
                SellerEmail = a.Seller?.Email,
                SellerAvatar = a.Seller?.AvatarUrl,
                SellerReputationScore = a.Seller?.ReputationScore,
                SellerTotalSales = a.Seller?.TotalSales,

                // Auction Info
                AuctionId = a.Id,
                StartingBid = a.StartingBid,
                CurrentBid = a.CurrentBid,
                BidCount = a.BidCount,
                AuctionStartTime = a.StartTime,
                AuctionEndTime = a.EndTime,
                AuctionStatus = a.Status
            }).ToList();
        }

        public async Task<AuctionCompletionResultDto> BuyNowAsync(int auctionId, int buyerId)
        {
            var auction = await _dbContext.Auctions
                .Include(a => a.Item)
                .FirstOrDefaultAsync(a => a.Id == auctionId)
                ?? throw new InvalidOperationException("Không tìm thấy phiên đấu giá");

            if (auction.BuyNowPrice == null)
            {
                throw new InvalidOperationException("Phiên đấu giá này không hỗ trợ mua ngay.");
            }

            if (!string.Equals(auction.Status, "active", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Phiên đấu giá không còn ở trạng thái hoạt động.");
            }

            if (auction.EndTime <= DateTime.Now)
            {
                throw new InvalidOperationException("Phiên đấu giá đã kết thúc.");
            }

            if (auction.WinnerId != null)
            {
                throw new InvalidOperationException("Phiên đấu giá đã có người chiến thắng.");
            }

            if (auction.SellerId == buyerId)
            {
                throw new InvalidOperationException("Người bán không thể mua sản phẩm của chính mình.");
            }

            var completedAt = DateTime.Now;

            var affected = await _dbContext.Auctions
                .Where(a => a.Id == auctionId && a.Status == auction.Status && a.WinnerId == null)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(a => a.Status, "completed")
                    .SetProperty(a => a.WinnerId, buyerId)
                    .SetProperty(a => a.CurrentBid, auction.BuyNowPrice)
                    .SetProperty(a => a.EndTime, completedAt));

            if (affected == 0)
            {
                throw new InvalidOperationException("Phiên đấu giá đã được hoàn tất trước đó.");
            }

            auction.Status = "completed";
            auction.WinnerId = buyerId;
            auction.CurrentBid = auction.BuyNowPrice;
            auction.EndTime = completedAt;

            await AddHistoryRecordIfMissingAsync(auction, auction.BuyNowPrice, completedAt);

            _dbContext.Entry(auction).State = EntityState.Detached;
            await _dbContext.SaveChangesAsync();

            // Set TTL cho Redis cache keys sau khi auction kết thúc
            if (_bidService != null)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _bidService.SetAuctionCacheExpirationAsync(auction.Id, expirationMinutes: 30);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to set Redis TTL for auction {AuctionId}", auction.Id);
                    }
                });
            }

            return new AuctionCompletionResultDto
            {
                AuctionId = auction.Id,
                WinnerId = buyerId,
                FinalPrice = auction.BuyNowPrice,
                Status = "completed",
                CompletionType = "buy-now",
                CompletedAt = completedAt
            };
        }

        public async Task<IReadOnlyList<AuctionCompletionResultDto>> FinalizeExpiredAuctionsAsync(CancellationToken cancellationToken = default)
        {
            var now = DateTime.Now;
            var expiredAuctions = await _dbContext.Auctions
                .Include(a => a.Item)
                .Where(a => a.Status == "active" && a.EndTime <= now)
                .ToListAsync(cancellationToken);

            if (expiredAuctions.Count == 0)
            {
                return Array.Empty<AuctionCompletionResultDto>();
            }

            var results = new List<AuctionCompletionResultDto>(expiredAuctions.Count);

            foreach (var auction in expiredAuctions)
            {
                try
                {
                    var (winnerId, finalBid) = await ResolveWinnerFromBidsAsync(auction.Id, cancellationToken);

                    // Update auction status and winner - giống BuyNowAsync
                    auction.Status = "completed";
                    auction.WinnerId = winnerId;
                    if (finalBid.HasValue)
                    {
                        auction.CurrentBid = finalBid.Value;
                    }
                    auction.EndTime = now; // Update EndTime giống BuyNowAsync

                    // Add history record - giống BuyNowAsync (trước khi save)
                    await AddHistoryRecordIfMissingAsync(auction, finalBid, now, cancellationToken);

                    var completion = new AuctionCompletionResultDto
                    {
                        AuctionId = auction.Id,
                        WinnerId = winnerId,
                        FinalPrice = finalBid,
                        Status = "completed",
                        CompletionType = "timeout",
                        CompletedAt = now
                    };

                    results.Add(completion);
                }
                catch (Exception ex)
                {
                    // Log error but continue processing other auctions
                    _logger?.LogError(ex, "Error finalizing auction {AuctionId}", auction.Id);
                }
            }

            // Save all changes at once - giống BuyNowAsync
            await _dbContext.SaveChangesAsync(cancellationToken);

            // Set TTL cho Redis cache keys sau khi auction kết thúc - giống BuyNowAsync
            if (_bidService != null)
            {
                foreach (var auction in expiredAuctions)
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _bidService.SetAuctionCacheExpirationAsync(auction.Id, expirationMinutes: 30);
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogWarning(ex, "Failed to set Redis TTL for auction {AuctionId}", auction.Id);
                        }
                    });
                }
            }

            return results;
        }

        private async Task<(int? winnerId, decimal? finalPrice)> ResolveWinnerFromBidsAsync(int auctionId, CancellationToken cancellationToken = default)
        {
            var highestBid = await _bidRepository.GetHighestBidByAuctionAsync(auctionId, cancellationToken);
            if (highestBid == null)
            {
                return (null, null);
            }

            return (highestBid.BidderId, highestBid.Amount);
        }

        private async Task AddHistoryRecordIfMissingAsync(Auction auction, decimal? finalBid, DateTime completedAt, CancellationToken cancellationToken = default)
        {
            var exists = await _dbContext.AuctionHistories
                .AnyAsync(h => h.AuctionId == auction.Id, cancellationToken);

            if (exists)
            {
                return;
            }

            var item = auction.Item ?? await _itemRepository.GetByIdAsync(auction.ItemId);

            var history = new AuctionHistory
            {
                AuctionId = auction.Id,
                ItemId = auction.ItemId,
                Title = item?.Title ?? $"Auction #{auction.Id}",
                CategoryId = item?.CategoryId ?? 0,
                SellerId = auction.SellerId,
                WinnerId = auction.WinnerId,
                StartingBid = auction.StartingBid,
                FinalBid = finalBid,
                TotalBids = auction.BidCount ?? 0,
                StartTime = auction.StartTime,
                EndTime = auction.EndTime,
                CompletedAt = completedAt
            };

            _dbContext.AuctionHistories.Add(history);
        }
    }
}
