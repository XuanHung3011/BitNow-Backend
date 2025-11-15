using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace BitNow_Backend.RealTime
{
	public class AuctionHub : Hub
	{
		public const string AdminDashboardGroup = "admin-dashboard";
		public const string AdminAuctionsGroup = "admin-auctions";
		public const string AdminPendingGroup = "admin-pending";
		public const string AdminAnalyticsGroup = "admin-analytics";

		public async Task JoinAuctionGroup(string auctionId)
		{
			await Groups.AddToGroupAsync(Context.ConnectionId, $"auction-{auctionId}");
		}

		public async Task LeaveAuctionGroup(string auctionId)
		{
			await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"auction-{auctionId}");
		}

		public Task JoinAdminChannel()
		{
			return Groups.AddToGroupAsync(Context.ConnectionId, AdminDashboardGroup);
		}

		public Task LeaveAdminChannel()
		{
			return Groups.RemoveFromGroupAsync(Context.ConnectionId, AdminDashboardGroup);
		}

		public Task JoinAdminSection(string section)
		{
			return Groups.AddToGroupAsync(Context.ConnectionId, GetAdminGroup(section));
		}

		public Task LeaveAdminSection(string section)
		{
			return Groups.RemoveFromGroupAsync(Context.ConnectionId, GetAdminGroup(section));
		}

		private static string GetAdminGroup(string? section)
		{
			return section?.ToLower() switch
			{
				"auctions" => AdminAuctionsGroup,
				"pending" => AdminPendingGroup,
				"analytics" => AdminAnalyticsGroup,
				"stats" => AdminDashboardGroup,
				_ => $"admin-{section?.ToLower() ?? "general"}"
			};
		}
	}
}


