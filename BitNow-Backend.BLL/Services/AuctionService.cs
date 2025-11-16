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
        private static readonly HashSet<string> AllowedStatuses = new(StringComparer.OrdinalIgnoreCase) { "draft", "active", "completed", "cancelled" };

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
				BidCount = a.BidCount
			};
		}

        public async Task<PaginatedResult<AuctionListItemDto>> GetAuctionsWithFilterAsync(AuctionFilterDto filter)
		{
			var (auctions, totalCount) = await _auctionRepository.GetAuctionsWithFilterAsync(filter);
			var now = DateTime.UtcNow;

			var items = auctions.Select(a =>
			{
				// Determine display status
				string displayStatus;
                if (a.Status != null && a.Status.Equals("cancelled", StringComparison.OrdinalIgnoreCase))
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
					EndTime = a.EndTime,
					Status = a.Status ?? "",
					DisplayStatus = displayStatus,
					BidCount = a.BidCount ?? 0
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

            // Validate dates
            if (dto.StartTime >= dto.EndTime)
            {
                throw new ArgumentException("Start time must be before end time");
            }

            if (dto.StartTime < DateTime.UtcNow)
            {
                throw new ArgumentException("Start time cannot be in the past");
            }

            // Create auction with active status (auction starts immediately)
            // Only set foreign key IDs, not navigation properties
            var auction = new Auction
            {
                ItemId = dto.ItemId,
                SellerId = dto.SellerId,
                StartingBid = dto.StartingBid,
                BuyNowPrice = dto.BuyNowPrice,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                Status = "active", // Set to active immediately when created
                BidCount = 0,
                CurrentBid = null,
                CreatedAt = DateTime.UtcNow,
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
    }
}
