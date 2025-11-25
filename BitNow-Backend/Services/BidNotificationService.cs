using BitNow_Backend.BLL.IServices;
using BitNow_Backend.DAL.DTOs;
using BitNow_Backend.RealTime;
using Microsoft.AspNetCore.SignalR;
using System;

namespace BitNow_Backend.Services
{
	/// <summary>
	/// Service để broadcast bid updates qua SignalR
	/// </summary>
	public class BidNotificationService : IBidNotificationService
	{
		private readonly IHubContext<AuctionHub> _hubContext;

		public BidNotificationService(IHubContext<AuctionHub> hubContext)
		{
			_hubContext = hubContext;
		}

		public async Task BroadcastBidPlacedAsync(int auctionId, BidResultDto bidResult)
		{
			try
			{
				// Broadcast đến tất cả clients trong group auction
				// Sử dụng All để đảm bảo broadcast đến tất cả clients, không chỉ group
				var groupName = $"auction-{auctionId}";
				await _hubContext.Clients.Group(groupName)
					.SendAsync("BidPlaced", bidResult);
				
				// Log để debug
				System.Diagnostics.Debug.WriteLine($"Broadcasted bid for auction {auctionId} to group {groupName}");
			}
			catch (Exception ex)
			{
				// Log error nhưng không throw để không ảnh hưởng đến bid
				System.Diagnostics.Debug.WriteLine($"SignalR broadcast error: {ex.Message}");
				System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
			}
		}
	}
}

