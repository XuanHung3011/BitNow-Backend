using BitNow_Backend.DAL.DTOs;

namespace BitNow_Backend.BLL.IServices
{
	public interface IAutoBidService
	{
		Task<AutoBidDto> CreateOrUpdateAutoBidAsync(int auctionId, int userId, decimal maxAmount);
		Task<bool> DeactivateAutoBidAsync(int auctionId, int userId);
		Task<AutoBidDto?> GetAutoBidAsync(int auctionId, int userId);
		Task ProcessAutoBidsAfterBidAsync(int auctionId, int currentBidderId, decimal currentBid);
		decimal CalculateBidIncrement(decimal currentPrice);
	}
}

