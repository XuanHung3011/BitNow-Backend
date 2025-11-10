using BitNow_Backend.BLL.IServices;
using BitNow_Backend.DAL.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace BitNow_Backend.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class MessagesController : ControllerBase
	{
		private readonly IMessageService _messageService;
		private readonly ILogger<MessagesController> _logger;

		public MessagesController(IMessageService messageService, ILogger<MessagesController> logger)
		{
			_messageService = messageService;
			_logger = logger;
		}

		/// <summary>
		/// Gửi tin nhắn
		/// </summary>
		[HttpPost]
		public async Task<ActionResult<MessageResponseDto>> SendMessage([FromBody] SendMessageRequest request)
		{
			try
			{
				if (!ModelState.IsValid)
					return BadRequest(ModelState);

				var message = await _messageService.SendMessageAsync(request);
				if (message == null)
					return BadRequest("Failed to send message");

				return Ok(message);
			}
			catch (ArgumentException ex)
			{
				return BadRequest(new { message = ex.Message });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error sending message");
				return StatusCode(500, new { message = "Internal server error" });
			}
		}

		/// <summary>
		/// Lấy danh sách hội thoại
		/// </summary>
		[HttpGet("conversations")]
		public async Task<ActionResult<IEnumerable<ConversationDto>>> GetConversations([FromQuery] int userId)
		{
			try
			{
				var conversations = await _messageService.GetConversationsAsync(userId);
				return Ok(conversations);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting conversations for user {UserId}", userId);
				return StatusCode(500, new { message = "Internal server error" });
			}
		}

		/// <summary>
		/// Lấy chi tiết cuộc hội thoại
		/// </summary>
		[HttpGet("conversation")]
		public async Task<ActionResult<IEnumerable<MessageResponseDto>>> GetConversation(
			[FromQuery] int userId1,
			[FromQuery] int userId2,
			[FromQuery] int? auctionId = null)
		{
			try
			{
				var messages = await _messageService.GetConversationAsync(userId1, userId2, auctionId);
				return Ok(messages);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting conversation between user {UserId1} and {UserId2}", userId1, userId2);
				return StatusCode(500, new { message = "Internal server error" });
			}
		}

		/// <summary>
		/// Đánh dấu tin nhắn đã đọc
		/// </summary>
		[HttpPut("{id}/read")]
		public async Task<ActionResult> MarkAsRead(int id)
		{
			try
			{
				var result = await _messageService.MarkAsReadAsync(id);
				if (!result)
					return NotFound(new { message = "Message not found" });

				return Ok(new { message = "Message marked as read" });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error marking message {MessageId} as read", id);
				return StatusCode(500, new { message = "Internal server error" });
			}
		}

		/// <summary>
		/// Lấy danh sách tin nhắn chưa đọc
		/// </summary>
		[HttpGet("unread")]
		public async Task<ActionResult<IEnumerable<MessageResponseDto>>> GetUnreadMessages([FromQuery] int userId)
		{
			try
			{
				var messages = await _messageService.GetUnreadMessagesAsync(userId);
				return Ok(messages);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting unread messages for user {UserId}", userId);
				return StatusCode(500, new { message = "Internal server error" });
			}
		}

		/// <summary>
		/// Lấy tất cả tin nhắn (đã gửi và đã nhận) của user
		/// </summary>
		[HttpGet("all")]
		public async Task<ActionResult<IEnumerable<MessageResponseDto>>> GetAllMessages([FromQuery] int userId)
		{
			try
			{
				var messages = await _messageService.GetAllMessagesByUserIdAsync(userId);
				return Ok(messages);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting all messages for user {UserId}", userId);
				return StatusCode(500, new { message = "Internal server error" });
			}
		}
	}
}

