using BitNow_Backend.BLL.IServices;
using BitNow_Backend.DAL.DTOs;
using BitNow_Backend.DAL.IRepositories;
using BitNow_Backend.DAL.Models;

namespace BitNow_Backend.BLL.Services
{
	public class MessageService : IMessageService
	{
		private readonly IMessageRepository _messageRepository;
		private readonly IUserService _userService;

		public MessageService(IMessageRepository messageRepository, IUserService userService)
		{
			_messageRepository = messageRepository;
			_userService = userService;
		}

		public async Task<MessageResponseDto?> SendMessageAsync(SendMessageRequest request)
		{
			if (string.IsNullOrWhiteSpace(request.Content))
				throw new ArgumentException("Message content cannot be empty");

			var message = new Message
			{
				SenderId = request.SenderId,
				ReceiverId = request.ReceiverId,
				AuctionId = request.AuctionId,
				Content = request.Content,
				IsRead = false,
				SentAt = DateTime.UtcNow
			};

			var savedMessage = await _messageRepository.AddAsync(message);
			return await GetMessageByIdAsync(savedMessage.Id);
		}

		public async Task<ConversationDto?> CreateConversationByEmailAsync(CreateConversationByEmailRequest request)
		{
			// Find receiver by email
			var receiver = await _userService.GetByEmailAsync(request.ReceiverEmail);
			if (receiver == null)
				throw new ArgumentException($"User with email {request.ReceiverEmail} not found");

			if (receiver.Id == request.SenderId)
				throw new ArgumentException("Cannot create conversation with yourself");

			// If initial message is provided, send it
			if (!string.IsNullOrWhiteSpace(request.InitialMessage))
			{
				var sendRequest = new SendMessageRequest
				{
					SenderId = request.SenderId,
					ReceiverId = receiver.Id,
					AuctionId = request.AuctionId,
					Content = request.InitialMessage
				};
				await SendMessageAsync(sendRequest);
			}

			// Check if conversation already exists by getting all conversations
			var allConversations = await GetConversationsAsync(request.SenderId);
			var existingConversation = allConversations.FirstOrDefault(c => 
				c.OtherUserId == receiver.Id && c.AuctionId == request.AuctionId);

			if (existingConversation != null)
			{
				// Return existing conversation
				return existingConversation;
			}

			// Return new conversation (even if no messages yet)
			return new ConversationDto
			{
				OtherUserId = receiver.Id,
				OtherUserName = receiver.FullName,
				OtherUserAvatarUrl = receiver.AvatarUrl,
				LastMessage = request.InitialMessage,
				LastMessageTime = string.IsNullOrWhiteSpace(request.InitialMessage) ? null : DateTime.UtcNow,
				UnreadCount = 0,
				AuctionId = request.AuctionId,
				AuctionTitle = null
			};
		}

		public async Task<IEnumerable<MessageResponseDto>> GetConversationAsync(int userId1, int userId2, int? auctionId = null)
		{
			var messages = await _messageRepository.GetConversationAsync(userId1, userId2, auctionId);
			return messages.Select(MapToDto);
		}

		public async Task<IEnumerable<ConversationDto>> GetConversationsAsync(int userId)
		{
			var conversations = await _messageRepository.GetConversationsAsync(userId);
			var result = new List<ConversationDto>();

			foreach (var message in conversations)
			{
				var otherUser = message.SenderId == userId ? message.Receiver : message.Sender;
				var otherUserId = message.SenderId == userId ? message.ReceiverId : message.SenderId;

				// Get unread count for this conversation
				var conversationMessages = await _messageRepository.GetConversationAsync(userId, otherUserId, message.AuctionId);
				var unreadCount = conversationMessages.Count(m => m.ReceiverId == userId && (m.IsRead == null || m.IsRead == false));

				// Get last message
				var lastMessage = conversationMessages.OrderByDescending(m => m.SentAt).FirstOrDefault();

				result.Add(new ConversationDto
				{
					OtherUserId = otherUserId,
					OtherUserName = otherUser.FullName,
					OtherUserAvatarUrl = otherUser.AvatarUrl,
					LastMessage = lastMessage?.Content,
					LastMessageTime = lastMessage?.SentAt,
					UnreadCount = unreadCount,
					AuctionId = message.AuctionId,
					AuctionTitle = message.Auction?.Item?.Title
				});
			}

			return result;
		}

		public async Task<IEnumerable<MessageResponseDto>> GetUnreadMessagesAsync(int userId)
		{
			var messages = await _messageRepository.GetUnreadMessagesAsync(userId);
			return messages.Select(MapToDto);
		}

		public async Task<bool> MarkAsReadAsync(int messageId)
		{
			return await _messageRepository.MarkAsReadAsync(messageId);
		}

		public async Task<bool> MarkConversationAsReadAsync(int userId1, int userId2, int? auctionId = null)
		{
			return await _messageRepository.MarkConversationAsReadAsync(userId1, userId2, auctionId);
		}

		public async Task<int> GetUnreadCountAsync(int userId)
		{
			return await _messageRepository.GetUnreadCountAsync(userId);
		}

		public async Task<MessageResponseDto?> GetMessageByIdAsync(int messageId)
		{
			var message = await _messageRepository.GetByIdAsync(messageId);
			return message == null ? null : MapToDto(message);
		}

		private static MessageResponseDto MapToDto(Message message)
		{
			return new MessageResponseDto
			{
				Id = message.Id,
				SenderId = message.SenderId,
				SenderName = message.Sender.FullName,
				SenderAvatarUrl = message.Sender.AvatarUrl,
				ReceiverId = message.ReceiverId,
				ReceiverName = message.Receiver.FullName,
				ReceiverAvatarUrl = message.Receiver.AvatarUrl,
				AuctionId = message.AuctionId,
				AuctionTitle = message.Auction?.Item?.Title,
				Content = message.Content,
				IsRead = message.IsRead ?? false,
				SentAt = message.SentAt
			};
		}
	}
}

