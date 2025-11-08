using System;

namespace BitNow_Backend.DAL.DTOs
{
	public class SendMessageRequest
	{
		public int SenderId { get; set; }
		public int ReceiverId { get; set; }
		public int? AuctionId { get; set; }
		public string Content { get; set; } = null!;
	}

	public class CreateConversationByEmailRequest
	{
		public int SenderId { get; set; }
		public string ReceiverEmail { get; set; } = null!;
		public int? AuctionId { get; set; }
		public string? InitialMessage { get; set; }
	}

	public class MessageResponseDto
	{
		public int Id { get; set; }
		public int SenderId { get; set; }
		public string SenderName { get; set; } = null!;
		public string? SenderAvatarUrl { get; set; }
		public int ReceiverId { get; set; }
		public string ReceiverName { get; set; } = null!;
		public string? ReceiverAvatarUrl { get; set; }
		public int? AuctionId { get; set; }
		public string? AuctionTitle { get; set; }
		public string Content { get; set; } = null!;
		public bool IsRead { get; set; }
		public DateTime? SentAt { get; set; }
	}

	public class ConversationDto
	{
		public int OtherUserId { get; set; }
		public string OtherUserName { get; set; } = null!;
		public string? OtherUserAvatarUrl { get; set; }
		public string? LastMessage { get; set; }
		public DateTime? LastMessageTime { get; set; }
		public int UnreadCount { get; set; }
		public int? AuctionId { get; set; }
		public string? AuctionTitle { get; set; }
	}
}

