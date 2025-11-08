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
		/// Send a message between users
		/// </summary>
		[HttpPost("send")]
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
		/// Get conversation between two users
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
		/// Get all conversations for a user
		/// </summary>
		[HttpGet("conversations/{userId}")]
		public async Task<ActionResult<IEnumerable<ConversationDto>>> GetConversations(int userId)
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
		/// Get unread messages for a user
		/// </summary>
		[HttpGet("unread/{userId}")]
		public async Task<ActionResult<IEnumerable<MessageResponseDto>>> GetUnreadMessages(int userId)
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
		/// Get unread message count for a user
		/// </summary>
		[HttpGet("unread-count/{userId}")]
		public async Task<ActionResult<int>> GetUnreadCount(int userId)
		{
			try
			{
				var count = await _messageService.GetUnreadCountAsync(userId);
				return Ok(new { count });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting unread count for user {UserId}", userId);
				return StatusCode(500, new { message = "Internal server error" });
			}
		}

		/// <summary>
		/// Mark a message as read
		/// </summary>
		[HttpPut("read/{messageId}")]
		public async Task<ActionResult> MarkAsRead(int messageId)
		{
			try
			{
				var result = await _messageService.MarkAsReadAsync(messageId);
				if (!result)
					return NotFound(new { message = "Message not found" });

				return Ok(new { message = "Message marked as read" });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error marking message {MessageId} as read", messageId);
				return StatusCode(500, new { message = "Internal server error" });
			}
		}

		/// <summary>
		/// Mark all messages in a conversation as read
		/// </summary>
		[HttpPut("conversation/read")]
		public async Task<ActionResult> MarkConversationAsRead(
			[FromQuery] int userId1,
			[FromQuery] int userId2,
			[FromQuery] int? auctionId = null)
		{
			try
			{
				var result = await _messageService.MarkConversationAsReadAsync(userId1, userId2, auctionId);
				if (!result)
					return BadRequest(new { message = "Failed to mark conversation as read" });

				return Ok(new { message = "Conversation marked as read" });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error marking conversation as read between user {UserId1} and {UserId2}", userId1, userId2);
				return StatusCode(500, new { message = "Internal server error" });
			}
		}

		/// <summary>
		/// Get a message by ID
		/// </summary>
		[HttpGet("{messageId}")]
		public async Task<ActionResult<MessageResponseDto>> GetMessage(int messageId)
		{
			try
			{
				var message = await _messageService.GetMessageByIdAsync(messageId);
				if (message == null)
					return NotFound(new { message = "Message not found" });

				return Ok(message);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting message {MessageId}", messageId);
				return StatusCode(500, new { message = "Internal server error" });
			}
		}

		/// <summary>
		/// Create a new conversation by email
		/// </summary>
		[HttpPost("create-conversation")]
		public async Task<ActionResult<ConversationDto>> CreateConversationByEmail([FromBody] CreateConversationByEmailRequest request)
		{
			try
			{
				if (!ModelState.IsValid)
					return BadRequest(ModelState);

				var conversation = await _messageService.CreateConversationByEmailAsync(request);
				if (conversation == null)
					return BadRequest("Failed to create conversation");

				return Ok(conversation);
			}
			catch (ArgumentException ex)
			{
				return BadRequest(new { message = ex.Message });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error creating conversation by email");
				return StatusCode(500, new { message = "Internal server error" });
			}
		}
	}
}

