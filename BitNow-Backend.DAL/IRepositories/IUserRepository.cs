using BitNow_Backend.DAL.Models;

namespace BitNow_Backend.DAL.IRepositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(int id);
    Task<User?> GetByEmailAsync(string email);
    Task<IEnumerable<User>> GetPagedAsync(int page, int pageSize);
    Task<int> CountAsync();
    Task<User> AddAsync(User user);
    Task UpdateAsync(User user);
    Task DeleteAsync(User user);
    Task<IEnumerable<User>> SearchAsync(string term, int page, int pageSize);
}


