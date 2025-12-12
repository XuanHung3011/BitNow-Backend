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
			return await GetConversationAsync(userId1, userId2, auctionId, null, null);
		}

		public async Task<IEnumerable<Message>> GetConversationAsync(int userId1, int userId2, int? auctionId = null, DateTime? fromDate = null, DateTime? toDate = null)
		{
			var query = _context.Messages
				.Include(m => m.Sender)
				.Include(m => m.Receiver)
				.Include(m => m.Auction)
					.ThenInclude(a => a!.Item)
				.Where(m => 
					((m.SenderId == userId1 && m.ReceiverId == userId2) ||
					 (m.SenderId == userId2 && m.ReceiverId == userId1)) &&
					(auctionId == null || m.AuctionId == auctionId) &&
					m.DisputeId == null); // Exclude dispute messages from regular conversations

			// Filter by date range if provided (for dispute chat filtering)
			if (fromDate.HasValue)
			{
				query = query.Where(m => m.SentAt >= fromDate.Value);
			}
			if (toDate.HasValue)
			{
				query = query.Where(m => m.SentAt < toDate.Value);
			}

			query = query.OrderBy(m => m.SentAt);

			return await query.ToListAsync();
		}

		public async Task<IEnumerable<Message>> GetMessagesByDisputeIdAsync(int disputeId)
		{
			return await _context.Messages
				.Include(m => m.Sender)
				.Include(m => m.Receiver)
				.Include(m => m.Auction)
					.ThenInclude(a => a!.Item)
				.Include(m => m.Dispute)
				.Where(m => m.DisputeId == disputeId)
				.OrderBy(m => m.SentAt)
				.ToListAsync();
		}

		public async Task<IEnumerable<Message>> GetConversationsAsync(int userId)
		{
			// Lấy tất cả messages liên quan đến user, sau đó group theo conversation
			// Chỉ lấy tin nhắn giữa hai user (loại bỏ bình luận public trong phiên đấu giá và dispute messages)
			var messages = await _context.Messages
				.Include(m => m.Sender)
				.Include(m => m.Receiver)
				.Include(m => m.Auction)
					.ThenInclude(a => a!.Item)
				.Where(m =>
					(m.SenderId == userId || m.ReceiverId == userId) &&
					m.AuctionId == null &&
					m.DisputeId == null) // Exclude dispute messages
				.OrderByDescending(m => m.SentAt)
				.ToListAsync();

			return messages;
		}

		public async Task<IEnumerable<Message>> GetUnreadMessagesAsync(int userId)
		{
			// Chỉ tính tin nhắn riêng giữa hai user, không tính bình luận phiên đấu giá và dispute messages
			return await _context.Messages
				.Include(m => m.Sender)
				.Include(m => m.Receiver)
				.Include(m => m.Auction)
					.ThenInclude(a => a!.Item)
				.Where(m =>
					m.ReceiverId == userId &&
					(m.IsRead == null || m.IsRead == false) &&
					m.AuctionId == null &&
					m.DisputeId == null) // Exclude dispute messages
				.OrderByDescending(m => m.SentAt)
				.ToListAsync();
		}

		public async Task<IEnumerable<Message>> GetAllMessagesByUserIdAsync(int userId)
		{
			// Chỉ lấy tin nhắn giữa hai user, loại bỏ bình luận phiên đấu giá và dispute messages
			return await _context.Messages
				.Include(m => m.Sender)
				.Include(m => m.Receiver)
				.Include(m => m.Auction)
					.ThenInclude(a => a!.Item)
				.Where(m =>
					(m.SenderId == userId || m.ReceiverId == userId) &&
					m.AuctionId == null &&
					m.DisputeId == null) // Exclude dispute messages
				.OrderByDescending(m => m.SentAt)
				.ToListAsync();
		}

		public async Task<IEnumerable<Message>> GetMessagesByAuctionAsync(int auctionId, int limit)
		{
			var normalizedLimit = limit <= 0 ? 100 : Math.Min(limit, 200);

			// CRITICAL: Only get auction chat messages (DisputeId must be null)
			// This ensures dispute messages are not shown in auction chat
			return await _context.Messages
				.Where(m => m.AuctionId == auctionId && m.DisputeId == null)
				.OrderBy(m => m.SentAt)
				.Take(normalizedLimit)
				.AsNoTracking()
				.ToListAsync();
		}

		public async Task<Message> AddAsync(Message message)
		{
			message.SentAt = DateTime.Now;
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
			// Chỉ đếm tin nhắn riêng, không đếm bình luận trong phiên đấu giá
			return await _context.Messages
				.CountAsync(m =>
					m.ReceiverId == userId &&
					(m.IsRead == null || m.IsRead == false) &&
					m.AuctionId == null);
		}
	}
}

