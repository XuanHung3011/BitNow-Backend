using BitNow_Backend.BLL.IServices;
using BitNow_Backend.DAL.DTOs;
using BitNow_Backend.DAL.IRepositories;

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
				SellerId = a.SellerId,
				StartingBid = a.StartingBid,
				CurrentBid = a.CurrentBid,
				BuyNowPrice = a.BuyNowPrice,
				StartTime = a.StartTime,
				EndTime = a.EndTime,
				Status = a.Status,
				BidCount = a.BidCount
			};
		}
	}
}
