using BitNow_Backend.DAL.Models;

namespace BitNow_Backend.DAL.IRepositories
{
	public interface IMessageRepository
	{
		Task<Message?> GetByIdAsync(int id);
		Task<IEnumerable<Message>> GetConversationAsync(int userId1, int userId2, int? auctionId = null);
		Task<IEnumerable<Message>> GetConversationAsync(int userId1, int userId2, int? auctionId = null, DateTime? fromDate = null, DateTime? toDate = null);
		Task<IEnumerable<Message>> GetMessagesByDisputeIdAsync(int disputeId);
		Task<IEnumerable<Message>> GetConversationsAsync(int userId);
		Task<IEnumerable<Message>> GetUnreadMessagesAsync(int userId);
		Task<IEnumerable<Message>> GetAllMessagesByUserIdAsync(int userId);
		Task<IEnumerable<Message>> GetMessagesByAuctionAsync(int auctionId, int limit);
		Task<Message> AddAsync(Message message);
		Task<bool> MarkAsReadAsync(int messageId);
		Task<int> GetUnreadCountAsync(int userId);
	}
}

