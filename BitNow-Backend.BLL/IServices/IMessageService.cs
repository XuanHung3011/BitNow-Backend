using BitNow_Backend.DAL.DTOs;

namespace BitNow_Backend.BLL.IServices
{
	public interface IMessageService
	{
		Task<MessageResponseDto?> SendMessageAsync(SendMessageRequest request);
		Task<ConversationDto?> CreateConversationByEmailAsync(CreateConversationByEmailRequest request);
		Task<IEnumerable<MessageResponseDto>> GetConversationAsync(int userId1, int userId2, int? auctionId = null);
		Task<IEnumerable<ConversationDto>> GetConversationsAsync(int userId);
		Task<IEnumerable<MessageResponseDto>> GetUnreadMessagesAsync(int userId);
		Task<bool> MarkAsReadAsync(int messageId);
		Task<bool> MarkConversationAsReadAsync(int userId1, int userId2, int? auctionId = null);
		Task<int> GetUnreadCountAsync(int userId);
		Task<MessageResponseDto?> GetMessageByIdAsync(int messageId);
	}
}

