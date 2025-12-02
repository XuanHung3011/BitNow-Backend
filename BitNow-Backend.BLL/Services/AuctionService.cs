using BitNow_Backend.BLL.IServices;
using BitNow_Backend.DAL.DTOs;
using BitNow_Backend.DAL.IRepositories;
using System;
using BitNow_Backend.DAL.Models;
using System.Collections.Generic;
using System.Linq;

namespace BitNow_Backend.BLL.Services
{
	public class AuctionService : IAuctionService
	{
        private readonly IAuctionRepository _auctionRepository;
        private readonly IItemRepository _itemRepository;
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

        public AuctionService(IAuctionRepository auctionRepository, IItemRepository itemRepository)
        {
            _auctionRepository = auctionRepository;
            _itemRepository = itemRepository;
        }

        public async Task<AuctionDetailDto?> GetDetailAsync(int id)
		{
			var a = await _auctionRepository.GetByIdAsync(id);
			if (a == null) return null;
			return new AuctionDetailDto
			{
				Id = a.Id,
				ItemId = a.ItemId,
				ItemTitle = a.Item.Title,
				ItemDescription = a.Item.Description,
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
				PausedAt = a.PausedAt
			};
		}

        public async Task<PaginatedResult<AuctionListItemDto>> GetAuctionsWithFilterAsync(AuctionFilterDto filter)
		{
			var (auctions, totalCount) = await _auctionRepository.GetAuctionsWithFilterAsync(filter);
			var now = DateTime.Now;

			var items = auctions.Select(a =>
			{
				// Determine display status
				string displayStatus;
                if (a.Status != null && a.Status.Equals("paused", StringComparison.OrdinalIgnoreCase))
                {
                    displayStatus = "paused";
                }
                else if (a.Status != null && a.Status.Equals("cancelled", StringComparison.OrdinalIgnoreCase))
                {
                    displayStatus = "cancelled";
				}
				else if (a.Status != null && a.Status.ToLower() == "active")
				{
					if (a.StartTime > now)
					{
						displayStatus = "scheduled";
					}
					else if (a.EndTime > now)
					{
						displayStatus = "active";
					}
					else
					{
						displayStatus = "completed";
					}
				}
				else if (a.EndTime < now || (a.Status != null && a.Status.ToLower() == "completed"))
				{
					displayStatus = "completed";
				}
				else
				{
					displayStatus = a.Status?.ToLower() ?? "unknown";
				}

				return new AuctionListItemDto
				{
					Id = a.Id,
					ItemTitle = a.Item?.Title ?? "",
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

            var items = auctions.Select(a =>
            {
                // Get user's highest bid (which should be the winning bid)
                var userBids = a.Bids?.Where(b => b.BidderId == bidderId).ToList() ?? new List<Bid>();
                var finalBid = userBids.Any() ? userBids.Max(b => b.Amount) : (a.CurrentBid ?? a.StartingBid);

                // Check if user has rated (you'll need to implement this based on your Rating system)
                // For now, defaulting to false
                var hasRated = false; // TODO: Check if rating exists for this auction and buyer

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
                    HasRated = hasRated
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
    }
}
