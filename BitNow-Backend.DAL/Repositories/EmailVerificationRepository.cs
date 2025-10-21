using BitNow_Backend.DAL.IRepositories;
using BitNow_Backend.DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace BitNow_Backend.DAL.Repositories;

public class EmailVerificationRepository : IEmailVerificationRepository
{
    private readonly BidNowDbContext _context;

    public EmailVerificationRepository(BidNowDbContext context)
    {
        _context = context;
    }

    public async Task<EmailVerification?> GetByTokenAsync(string token)
    {
        return await _context.EmailVerifications.FirstOrDefaultAsync(v => v.Token == token);
    }

    public async Task<EmailVerification?> GetLatestByEmailAsync(string email)
    {
        return await _context.EmailVerifications
            .Where(v => v.Email == email)
            .OrderByDescending(v => v.Id)
            .FirstOrDefaultAsync();
    }

    public async Task AddAsync(EmailVerification verification)
    {
        _context.EmailVerifications.Add(verification);
        await _context.SaveChangesAsync();
    }

    public async Task MarkUsedAsync(EmailVerification verification)
    {
        verification.IsUsed = true;
        _context.EmailVerifications.Update(verification);
        await _context.SaveChangesAsync();
    }
}



