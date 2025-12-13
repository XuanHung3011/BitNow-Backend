using BitNow_Backend.DAL.DTOs;

namespace BitNow_Backend.BLL.IServices
{
	public interface IMessageService
	{
		Task<MessageResponseDto?> SendMessageAsync(SendMessageRequest request);
		Task<IEnumerable<ConversationDto>> GetConversationsAsync(int userId);
	Task<IEnumerable<MessageResponseDto>> GetConversationAsync(int userId1, int userId2, int? auctionId = null);
	Task<IEnumerable<MessageResponseDto>> GetConversationAsync(int userId1, int userId2, int? auctionId = null, DateTime? fromDate = null, DateTime? toDate = null);
	Task<IEnumerable<MessageResponseDto>> GetDisputeMessagesAsync(int disputeId, int currentUserId);
	Task<bool> MarkAsReadAsync(int messageId);
	Task<IEnumerable<MessageResponseDto>> GetUnreadMessagesAsync(int userId);
	Task<IEnumerable<MessageResponseDto>> GetAllMessagesByUserIdAsync(int userId);
	}
}

