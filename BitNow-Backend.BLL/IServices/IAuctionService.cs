using BitNow_Backend.DAL.DTOs;

namespace BitNow_Backend.BLL.IServices
{
	public interface IAuctionService
	{
		Task<AuctionDetailDto?> GetDetailAsync(int id);
	}
}
