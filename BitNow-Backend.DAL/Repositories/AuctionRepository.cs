using BitNow_Backend.DAL.IRepositories;
using BitNow_Backend.DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace BitNow_Backend.DAL.Repositories
{
	public class AuctionRepository : IAuctionRepository
	{
		private readonly BidNowDbContext _context;

		public AuctionRepository(BidNowDbContext context)
		{
			_context = context;
		}

		public async Task<Auction?> GetByIdAsync(int id)
		{
			return await _context.Auctions
				.Include(a => a.Item)
                .ThenInclude(i => i.Category)
                .Include(a => a.Seller)
                .FirstOrDefaultAsync(a => a.Id == id);
		}
	}
}
