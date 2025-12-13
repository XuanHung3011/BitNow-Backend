using System.Linq;
using BitNow_Backend.BLL.IServices;
using BitNow_Backend.DAL.DTOs;
using BitNow_Backend.DAL.IRepositories;
using BitNow_Backend.DAL.Models;

namespace BitNow_Backend.BLL.Services
{
	public class AuctionChatService : IAuctionChatService
	{
		private readonly IMessageRepository _messageRepository;
		private readonly IAuctionRepository _auctionRepository;
		private readonly IUserRepository _userRepository;

		public AuctionChatService(
			IMessageRepository messageRepository,
			IAuctionRepository auctionRepository,
			IUserRepository userRepository)
		{
			_messageRepository = messageRepository;
			_auctionRepository = auctionRepository;
			_userRepository = userRepository;
		}

		public async Task<IEnumerable<AuctionChatMessageDto>> GetMessagesAsync(int auctionId, int limit = 100, int? viewerId = null)
		{
			if (auctionId <= 0)
			{
				throw new ArgumentException("AuctionId must be greater than zero.", nameof(auctionId));
			}

			var normalizedLimit = limit <= 0 ? 100 : Math.Min(limit, 200);
			var messages = await _messageRepository.GetMessagesByAuctionAsync(auctionId, normalizedLimit);

			return messages.Select(message => MapToDto(message, viewerId));
		}

		public async Task<AuctionChatMessageDto> AddMessageAsync(CreateAuctionChatMessageRequest request)
		{
			if (request == null)
			{
				throw new ArgumentException("Request cannot be null.");
			}

			if (request.AuctionId <= 0)
			{
				throw new ArgumentException("AuctionId is required.");
			}

			if (request.SenderId <= 0)
			{
				throw new ArgumentException("SenderId is required.");
			}

			if (string.IsNullOrWhiteSpace(request.Content))
			{
				throw new ArgumentException("Content is required.");
			}

			var trimmedContent = request.Content.Trim();
			if (trimmedContent.Length > 1000)
			{
				throw new ArgumentException("Content is too long. Maximum 1000 characters.");
			}

			var auction = await _auctionRepository.GetByIdAsync(request.AuctionId);
			if (auction == null)
			{
				throw new ArgumentException("Auction not found.");
			}

			var sender = await _userRepository.GetByIdAsync(request.SenderId);
			if (sender == null)
			{
				throw new ArgumentException("Sender not found.");
			}

			var receiverId = auction.SellerId;
			if (receiverId <= 0)
			{
				throw new ArgumentException("Auction does not have a valid seller.");
			}

			var message = new Message
			{
				AuctionId = request.AuctionId,
				DisputeId = null, // CRITICAL: Auction chat messages should never have DisputeId
				SenderId = request.SenderId,
				ReceiverId = receiverId,
				Content = trimmedContent,
				SentAt = DateTime.Now,
				IsRead = false
			};

			var savedMessage = await _messageRepository.AddAsync(message);
			return MapToDto(savedMessage, request.SenderId);
		}

		private static AuctionChatMessageDto MapToDto(Message message, int? viewerId)
		{
			return new AuctionChatMessageDto
			{
				Id = message.Id,
				Alias = BuildAlias(message.SenderId),
				Content = message.Content,
				SentAt = message.SentAt,
				IsMine = viewerId.HasValue && message.SenderId == viewerId.Value
			};
		}

		private static string BuildAlias(int senderId)
		{
			var sanitized = Math.Abs(senderId).ToString();
			var suffix = sanitized.Length <= 4 ? sanitized.PadLeft(4, '0') : sanitized[^4..];
			return $"Người dùng #{suffix}";
		}
	}
}


