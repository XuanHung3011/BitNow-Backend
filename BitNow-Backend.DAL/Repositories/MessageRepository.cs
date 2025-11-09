using BitNow_Backend.DAL.IRepositories;
using BitNow_Backend.DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace BitNow_Backend.DAL.Repositories
{
	public class MessageRepository : IMessageRepository
	{
		private readonly BidNowDbContext _context;

		public MessageRepository(BidNowDbContext context)
		{
			_context = context;
		}

		public async Task<Message?> GetByIdAsync(int id)
		{
			return await _context.Messages
				.Include(m => m.Sender)
				.Include(m => m.Receiver)
				.Include(m => m.Auction)
					.ThenInclude(a => a!.Item)
				.FirstOrDefaultAsync(m => m.Id == id);
		}

		public async Task<IEnumerable<Message>> GetConversationAsync(int userId1, int userId2, int? auctionId = null)
		{
			var query = _context.Messages
				.Include(m => m.Sender)
				.Include(m => m.Receiver)
				.Include(m => m.Auction)
					.ThenInclude(a => a!.Item)
				.Where(m => 
					((m.SenderId == userId1 && m.ReceiverId == userId2) ||
					 (m.SenderId == userId2 && m.ReceiverId == userId1)) &&
					(auctionId == null || m.AuctionId == auctionId))
				.OrderBy(m => m.SentAt);

			return await query.ToListAsync();
		}

		public async Task<IEnumerable<Message>> GetConversationsAsync(int userId)
		{
			// Lấy tất cả messages liên quan đến user, sau đó group theo conversation
			var messages = await _context.Messages
				.Include(m => m.Sender)
				.Include(m => m.Receiver)
				.Include(m => m.Auction)
					.ThenInclude(a => a!.Item)
				.Where(m => m.SenderId == userId || m.ReceiverId == userId)
				.OrderByDescending(m => m.SentAt)
				.ToListAsync();

			return messages;
		}

		public async Task<IEnumerable<Message>> GetUnreadMessagesAsync(int userId)
		{
			return await _context.Messages
				.Include(m => m.Sender)
				.Include(m => m.Receiver)
				.Include(m => m.Auction)
					.ThenInclude(a => a!.Item)
				.Where(m => m.ReceiverId == userId && (m.IsRead == null || m.IsRead == false))
				.OrderByDescending(m => m.SentAt)
				.ToListAsync();
		}

		public async Task<IEnumerable<Message>> GetAllMessagesByUserIdAsync(int userId)
		{
			return await _context.Messages
				.Include(m => m.Sender)
				.Include(m => m.Receiver)
				.Include(m => m.Auction)
					.ThenInclude(a => a!.Item)
				.Where(m => m.SenderId == userId || m.ReceiverId == userId)
				.OrderByDescending(m => m.SentAt)
				.ToListAsync();
		}

		public async Task<Message> AddAsync(Message message)
		{
			message.SentAt = DateTime.UtcNow;
			message.IsRead = false;
			_context.Messages.Add(message);
			await _context.SaveChangesAsync();
			return message;
		}

		public async Task<bool> MarkAsReadAsync(int messageId)
		{
			var message = await _context.Messages.FindAsync(messageId);
			if (message == null) return false;

			message.IsRead = true;
			await _context.SaveChangesAsync();
			return true;
		}

		public async Task<int> GetUnreadCountAsync(int userId)
		{
			return await _context.Messages
				.CountAsync(m => m.ReceiverId == userId && (m.IsRead == null || m.IsRead == false));
		}
	}
}

