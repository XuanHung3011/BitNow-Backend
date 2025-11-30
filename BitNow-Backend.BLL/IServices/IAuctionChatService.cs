using BitNow_Backend.DAL.DTOs;

namespace BitNow_Backend.BLL.IServices
{
	public interface IAuctionChatService
	{
		Task<IEnumerable<AuctionChatMessageDto>> GetMessagesAsync(int auctionId, int limit = 100, int? viewerId = null);
		Task<AuctionChatMessageDto> AddMessageAsync(CreateAuctionChatMessageRequest request);
	}
}


