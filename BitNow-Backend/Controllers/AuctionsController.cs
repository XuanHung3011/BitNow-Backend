using BitNow_Backend.BLL.IServices;
using BitNow_Backend.DAL.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace BitNow_Backend.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class AuctionsController : ControllerBase
	{
		private readonly IAuctionService _auctionService;

		public AuctionsController(IAuctionService auctionService)
		{
			_auctionService = auctionService;
		}

		[HttpGet("{id}")]
		public async Task<ActionResult<AuctionDetailDto>> Get(int id)
		{
			var dto = await _auctionService.GetDetailAsync(id);
			if (dto == null) return NotFound();
			return Ok(dto);
		}
	}
}
