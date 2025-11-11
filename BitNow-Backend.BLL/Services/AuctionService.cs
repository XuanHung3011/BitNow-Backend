using BitNow_Backend.BLL.IServices;
using BitNow_Backend.DAL.DTOs;
using BitNow_Backend.DAL.IRepositories;
using System;
using System.Linq;

namespace BitNow_Backend.BLL.Services
{
	public class AuctionService : IAuctionService
	{
		private readonly IAuctionRepository _auctionRepository;

		public AuctionService(IAuctionRepository auctionRepository)
		{
			_auctionRepository = auctionRepository;
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
				if (a.Status != null && a.Status.ToLower() == "suspended")
				{
					displayStatus = "suspended";
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
	}
}
