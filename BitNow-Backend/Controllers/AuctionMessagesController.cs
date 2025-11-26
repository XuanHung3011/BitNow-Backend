using BitNow_Backend.BLL.IServices;
using BitNow_Backend.DAL.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using BitNow_Backend.RealTime;

namespace BitNow_Backend.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class AuctionMessagesController : ControllerBase
	{
		private readonly IAuctionChatService _auctionChatService;
		private readonly ILogger<AuctionMessagesController> _logger;
		private readonly IHubContext<MessageHub> _hubContext;

		public AuctionMessagesController(
			IAuctionChatService auctionChatService, 
			ILogger<AuctionMessagesController> logger,
			IHubContext<MessageHub> hubContext)
		{
			_auctionChatService = auctionChatService;
			_logger = logger;
			_hubContext = hubContext;
		}

		[HttpGet("{auctionId:int}")]
		public async Task<ActionResult<IEnumerable<AuctionChatMessageDto>>> GetMessages(int auctionId, [FromQuery] int limit = 100, [FromQuery] int? viewerId = null)
		{
			try
			{
				var messages = await _auctionChatService.GetMessagesAsync(auctionId, limit, viewerId);
				return Ok(messages);
			}
			catch (ArgumentException ex)
			{
				return BadRequest(new { message = ex.Message });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting auction messages for auction {AuctionId}", auctionId);
				return StatusCode(500, new { message = "Internal server error" });
			}
		}

		[HttpPost]
		public async Task<ActionResult<AuctionChatMessageDto>> CreateMessage([FromBody] CreateAuctionChatMessageRequest request)
		{
			try
			{
				// DTO trả về cho người gửi (isMine = true cho sender)
				var message = await _auctionChatService.AddMessageAsync(request);

				// DTO broadcast real-time cho tất cả clients (isMine luôn false, client sẽ tự xác định)
				var broadcastMessage = new AuctionChatMessageDto
				{
					Id = message.Id,
					Alias = message.Alias,
					Content = message.Content,
					SentAt = message.SentAt,
					IsMine = false
				};

				var groupName = $"auction-chat-{request.AuctionId}";
				_logger.LogInformation("Broadcasting auction chat message {MessageId} to group {GroupName}", message.Id, groupName);

				// Broadcast message mới qua SignalR đến tất cả users trong auction chat group
				await _hubContext.Clients.Group(groupName)
					.SendAsync("AuctionChatMessageReceived", broadcastMessage);

				_logger.LogInformation("Successfully broadcasted auction chat message {MessageId} to group {GroupName}", message.Id, groupName);

				// Trả về DTO gốc cho người gọi API (sender thấy isMine = true ngay lập tức)
				return Ok(message);
			}
			catch (ArgumentException ex)
			{
				return BadRequest(new { message = ex.Message });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error creating auction message for auction {AuctionId}", request.AuctionId);
				return StatusCode(500, new { message = "Internal server error" });
			}
		}
	}
}


