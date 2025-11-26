using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace BitNow_Backend.RealTime
{
	public class MessageHub : Hub
	{
		/// <summary>
		/// Mỗi người dùng join vào group riêng dạng user-{userId}
		/// để nhận tin nhắn realtime (bao gồm cả sender và receiver).
		/// </summary>
		public async Task JoinUserGroup(string userId)
		{
			if (string.IsNullOrWhiteSpace(userId)) return;
			await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");
		}

		public async Task LeaveUserGroup(string userId)
		{
			if (string.IsNullOrWhiteSpace(userId)) return;
			await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user-{userId}");
		}

		/// <summary>
		/// Join vào group chat của một auction để nhận tin nhắn realtime
		/// </summary>
		public async Task JoinAuctionChatGroup(int auctionId)
		{
			await Groups.AddToGroupAsync(Context.ConnectionId, $"auction-chat-{auctionId}");
		}

		/// <summary>
		/// Rời khỏi group chat của một auction
		/// </summary>
		public async Task LeaveAuctionChatGroup(int auctionId)
		{
			await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"auction-chat-{auctionId}");
		}
	}
}


