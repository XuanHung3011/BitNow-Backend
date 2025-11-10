using BitNow_Backend.BLL.IServices;
using BitNow_Backend.DAL.DTOs;
using BitNow_Backend.DAL.IRepositories;
using BitNow_Backend.DAL.Models;

namespace BitNow_Backend.BLL.Services
{
	public class MessageService : IMessageService
	{
		private readonly IMessageRepository _messageRepository;
		private readonly IUserRepository _userRepository;

		public MessageService(IMessageRepository messageRepository, IUserRepository userRepository)
		{
			_messageRepository = messageRepository;
			_userRepository = userRepository;
		}

		public async Task<MessageResponseDto?> SendMessageAsync(SendMessageRequest request)
		{
			if (request.SenderId == request.ReceiverId)
				throw new ArgumentException("Cannot send message to yourself");

			var sender = await _userRepository.GetByIdAsync(request.SenderId);
			var receiver = await _userRepository.GetByIdAsync(request.ReceiverId);

			if (sender == null || receiver == null)
				throw new ArgumentException("Sender or receiver not found");

			var message = new Message
			{
				SenderId = request.SenderId,
				ReceiverId = request.ReceiverId,
				AuctionId = request.AuctionId,
				Content = request.Content,
				SentAt = DateTime.UtcNow,
				IsRead = false
			};

			var savedMessage = await _messageRepository.AddAsync(message);
			return await MapToMessageResponseDto(savedMessage);
		}

		public async Task<IEnumerable<ConversationDto>> GetConversationsAsync(int userId)
		{
			var messages = await _messageRepository.GetConversationsAsync(userId);
			
			// Group messages by conversation (other user + auction)
			var conversations = messages
				.GroupBy(m => new
				{
					OtherUserId = m.SenderId == userId ? m.ReceiverId : m.SenderId,
					AuctionId = m.AuctionId
				})
				.Select(g =>
				{
					var lastMessage = g.OrderByDescending(m => m.SentAt).First();
					var otherUserId = g.Key.OtherUserId;
					var otherUser = lastMessage.SenderId == userId ? lastMessage.Receiver : lastMessage.Sender;
					
					return new ConversationDto
					{
						OtherUserId = otherUserId,
						OtherUserName = otherUser?.FullName ?? "Unknown",
						OtherUserAvatarUrl = otherUser?.AvatarUrl,
						LastMessage = lastMessage.Content,
						LastMessageTime = lastMessage.SentAt,
						UnreadCount = g.Count(m => m.ReceiverId == userId && (m.IsRead == null || m.IsRead == false)),
						AuctionId = g.Key.AuctionId,
						AuctionTitle = lastMessage.Auction?.Item?.Title
					};
				})
				.OrderByDescending(c => c.LastMessageTime)
				.ToList();

			return conversations;
		}

		public async Task<IEnumerable<MessageResponseDto>> GetConversationAsync(int userId1, int userId2, int? auctionId = null)
		{
			var messages = await _messageRepository.GetConversationAsync(userId1, userId2, auctionId);
			var result = new List<MessageResponseDto>();

			foreach (var message in messages)
			{
				var dto = await MapToMessageResponseDto(message);
				if (dto != null)
					result.Add(dto);
			}

			return result;
		}

		public async Task<bool> MarkAsReadAsync(int messageId)
		{
			return await _messageRepository.MarkAsReadAsync(messageId);
		}

		public async Task<IEnumerable<MessageResponseDto>> GetUnreadMessagesAsync(int userId)
		{
			var messages = await _messageRepository.GetUnreadMessagesAsync(userId);
			var result = new List<MessageResponseDto>();

			foreach (var message in messages)
			{
				var dto = await MapToMessageResponseDto(message);
				if (dto != null)
					result.Add(dto);
			}

			return result;
		}

		public async Task<IEnumerable<MessageResponseDto>> GetAllMessagesByUserIdAsync(int userId)
		{
			var messages = await _messageRepository.GetAllMessagesByUserIdAsync(userId);
			var result = new List<MessageResponseDto>();

			foreach (var message in messages)
			{
				var dto = await MapToMessageResponseDto(message);
				if (dto != null)
					result.Add(dto);
			}

			return result;
		}

		private async Task<MessageResponseDto?> MapToMessageResponseDto(Message message)
		{
			if (message == null) return null;

			var sender = await _userRepository.GetByIdAsync(message.SenderId);
			var receiver = await _userRepository.GetByIdAsync(message.ReceiverId);

			return new MessageResponseDto
			{
				Id = message.Id,
				SenderId = message.SenderId,
				SenderName = sender?.FullName ?? "Unknown",
				SenderAvatarUrl = sender?.AvatarUrl,
				ReceiverId = message.ReceiverId,
				ReceiverName = receiver?.FullName ?? "Unknown",
				ReceiverAvatarUrl = receiver?.AvatarUrl,
				AuctionId = message.AuctionId,
				AuctionTitle = message.Auction?.Item?.Title,
				Content = message.Content,
				IsRead = message.IsRead ?? false,
				SentAt = message.SentAt
			};
		}
	}
}

