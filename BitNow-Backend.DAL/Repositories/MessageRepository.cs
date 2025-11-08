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
					.ThenInclude(a => a != null ? a.Item : null!)
				.FirstOrDefaultAsync(m => m.Id == id);
		}

		public async Task<IEnumerable<Message>> GetConversationAsync(int userId1, int userId2, int? auctionId = null)
		{
			var query = _context.Messages
				.Include(m => m.Sender)
				.Include(m => m.Receiver)
				.Include(m => m.Auction)
					.ThenInclude(a => a != null ? a.Item : null!)
				.Where(m => (m.SenderId == userId1 && m.ReceiverId == userId2) ||
							(m.SenderId == userId2 && m.ReceiverId == userId1));

			if (auctionId.HasValue)
			{
				query = query.Where(m => m.AuctionId == auctionId.Value);
			}

			return await query
				.OrderBy(m => m.SentAt)
				.ToListAsync();
		}

		public async Task<IEnumerable<Message>> GetMessagesByUserAsync(int userId)
		{
			return await _context.Messages
				.Include(m => m.Sender)
				.Include(m => m.Receiver)
				.Include(m => m.Auction)
					.ThenInclude(a => a != null ? a.Item : null!)
				.Where(m => m.SenderId == userId || m.ReceiverId == userId)
				.OrderByDescending(m => m.SentAt)
				.ToListAsync();
		}

		public async Task<IEnumerable<Message>> GetUnreadMessagesAsync(int userId)
		{
			return await _context.Messages
				.Include(m => m.Sender)
				.Include(m => m.Receiver)
				.Include(m => m.Auction)
					.ThenInclude(a => a != null ? a.Item : null!)
				.Where(m => m.ReceiverId == userId && (m.IsRead == null || m.IsRead == false))
				.OrderByDescending(m => m.SentAt)
				.ToListAsync();
		}

		public async Task<Message> AddAsync(Message entity)
		{
			_context.Messages.Add(entity);
			await _context.SaveChangesAsync();
			return entity;
		}

		public async Task<bool> MarkAsReadAsync(int messageId)
		{
			var message = await _context.Messages.FindAsync(messageId);
			if (message == null) return false;

			message.IsRead = true;
			await _context.SaveChangesAsync();
			return true;
		}

		public async Task<bool> MarkConversationAsReadAsync(int userId1, int userId2, int? auctionId = null)
		{
			var query = _context.Messages
				.Where(m => m.ReceiverId == userId1 &&
							m.SenderId == userId2 &&
							(m.IsRead == null || m.IsRead == false));

			if (auctionId.HasValue)
			{
				query = query.Where(m => m.AuctionId == auctionId.Value);
			}

			var messages = await query.ToListAsync();
			foreach (var message in messages)
			{
				message.IsRead = true;
			}

			if (messages.Count > 0)
			{
				await _context.SaveChangesAsync();
			}

			return true;
		}

		public async Task<int> GetUnreadCountAsync(int userId)
		{
			return await _context.Messages
				.CountAsync(m => m.ReceiverId == userId && (m.IsRead == null || m.IsRead == false));
		}

		public async Task<IEnumerable<Message>> GetConversationsAsync(int userId)
		{
			// Get the most recent message from each conversation
			var conversations = await _context.Messages
				.Include(m => m.Sender)
				.Include(m => m.Receiver)
				.Include(m => m.Auction)
					.ThenInclude(a => a != null ? a.Item : null!)
				.Where(m => m.SenderId == userId || m.ReceiverId == userId)
				.GroupBy(m => new
				{
					OtherUserId = m.SenderId == userId ? m.ReceiverId : m.SenderId,
					m.AuctionId
				})
				.Select(g => g.OrderByDescending(m => m.SentAt).First())
				.OrderByDescending(m => m.SentAt)
				.ToListAsync();

			return conversations;
		}
	}
}

