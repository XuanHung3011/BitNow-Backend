using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace BitNow_Backend.RealTime
{
	public class AuctionHub : Hub
	{
		public async Task JoinAuctionGroup(string auctionId)
		{
			await Groups.AddToGroupAsync(Context.ConnectionId, $"auction-{auctionId}");
		}

		public async Task LeaveAuctionGroup(string auctionId)
		{
			await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"auction-{auctionId}");
		}

		/// <summary>
		/// Join admin section group for real-time updates (stats, pending items, analytics, etc.)
		/// </summary>
		/// <param name="section">Admin section name: "stats", "pending", "auctions", "analytics"</param>
		public async Task JoinAdminSection(string section)
		{
			await Groups.AddToGroupAsync(Context.ConnectionId, $"admin-{section}");
		}

		/// <summary>
		/// Leave admin section group
		/// </summary>
		/// <param name="section">Admin section name</param>
		public async Task LeaveAdminSection(string section)
		{
			await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"admin-{section}");
		}
	}
}


