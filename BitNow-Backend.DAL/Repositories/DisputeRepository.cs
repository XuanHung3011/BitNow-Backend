using BitNow_Backend.DAL.IRepositories;
using BitNow_Backend.DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace BitNow_Backend.DAL.Repositories;

public class DisputeRepository : IDisputeRepository
{
    private readonly BidNowDbContext _context;

    public DisputeRepository(BidNowDbContext context)
    {
        _context = context;
    }

    public async Task<Dispute?> GetByIdAsync(int id)
    {
        return await _context.Disputes
            .Include(d => d.Order)
                .ThenInclude(o => o.Auction)
                    .ThenInclude(a => a.Item)
            .Include(d => d.Buyer)
            .Include(d => d.Seller)
            .Include(d => d.Resolver)
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task<Dispute?> GetByOrderIdAsync(int orderId)
    {
        return await _context.Disputes
            .Include(d => d.Order)
                .ThenInclude(o => o.Auction)
                    .ThenInclude(a => a.Item)
            .Include(d => d.Buyer)
            .Include(d => d.Seller)
            .Include(d => d.Resolver)
            .FirstOrDefaultAsync(d => d.OrderId == orderId);
    }

    public async Task<IEnumerable<Dispute>> GetAllAsync()
    {
        return await _context.Disputes
            .Include(d => d.Order)
                .ThenInclude(o => o.Auction)
                    .ThenInclude(a => a.Item)
            .Include(d => d.Buyer)
            .Include(d => d.Seller)
            .Include(d => d.Resolver)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Dispute>> GetByStatusAsync(string status)
    {
        return await _context.Disputes
            .Include(d => d.Order)
                .ThenInclude(o => o.Auction)
                    .ThenInclude(a => a.Item)
            .Include(d => d.Buyer)
            .Include(d => d.Seller)
            .Include(d => d.Resolver)
            .Where(d => d.Status == status)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Dispute>> GetByBuyerIdAsync(int buyerId)
    {
        return await _context.Disputes
            .Include(d => d.Order)
                .ThenInclude(o => o.Auction)
                    .ThenInclude(a => a.Item)
            .Include(d => d.Buyer)
            .Include(d => d.Seller)
            .Include(d => d.Resolver)
            .Where(d => d.BuyerId == buyerId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Dispute>> GetBySellerIdAsync(int sellerId)
    {
        return await _context.Disputes
            .Include(d => d.Order)
                .ThenInclude(o => o.Auction)
                    .ThenInclude(a => a.Item)
            .Include(d => d.Buyer)
            .Include(d => d.Seller)
            .Include(d => d.Resolver)
            .Where(d => d.SellerId == sellerId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();
    }

    public async Task<Dispute> AddAsync(Dispute dispute)
    {
        _context.Disputes.Add(dispute);
        await _context.SaveChangesAsync();
        return dispute;
    }

    public async Task UpdateAsync(Dispute dispute)
    {
        _context.Disputes.Update(dispute);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Dispute dispute)
    {
        _context.Disputes.Remove(dispute);
        await _context.SaveChangesAsync();
    }
}

