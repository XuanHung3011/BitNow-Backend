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
	}
}


