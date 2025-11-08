using BitNow_Backend.DAL.Models;

namespace BitNow_Backend.DAL.IRepositories
{
	public interface IMessageRepository
	{
		Task<Message?> GetByIdAsync(int id);
		Task<IEnumerable<Message>> GetConversationAsync(int userId1, int userId2, int? auctionId = null);
		Task<IEnumerable<Message>> GetMessagesByUserAsync(int userId);
		Task<IEnumerable<Message>> GetUnreadMessagesAsync(int userId);
		Task<Message> AddAsync(Message entity);
		Task<bool> MarkAsReadAsync(int messageId);
		Task<bool> MarkConversationAsReadAsync(int userId1, int userId2, int? auctionId = null);
		Task<int> GetUnreadCountAsync(int userId);
		Task<IEnumerable<Message>> GetConversationsAsync(int userId);
	}
}

