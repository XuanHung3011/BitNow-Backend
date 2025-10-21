using BitNow_Backend.DAL.Models;

namespace BitNow_Backend.DAL.IRepositories;

public interface IEmailVerificationRepository
{
    Task<EmailVerification?> GetByTokenAsync(string token);
    Task<EmailVerification?> GetLatestByEmailAsync(string email);
    Task AddAsync(EmailVerification verification);
    Task MarkUsedAsync(EmailVerification verification);
}


