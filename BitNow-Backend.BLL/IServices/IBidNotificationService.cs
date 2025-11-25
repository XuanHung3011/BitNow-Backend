using BitNow_Backend.DAL.DTOs;

namespace BitNow_Backend.BLL.IServices
{
	/// <summary>
	/// Interface để broadcast bid updates (SignalR hoặc các phương thức khác)
	/// </summary>
	public interface IBidNotificationService
	{
		Task BroadcastBidPlacedAsync(int auctionId, BidResultDto bidResult);
	}
}

