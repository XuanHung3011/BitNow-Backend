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
	}
}


