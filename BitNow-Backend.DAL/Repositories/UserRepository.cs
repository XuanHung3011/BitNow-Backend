using BitNow_Backend.DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace BitNow_Backend.DAL.Repositories;

public class UserRepository : BitNow_Backend.DAL.IRepositories.IUserRepository
{
    private readonly BidNowDbContext _context;

    public UserRepository(BidNowDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(int id)
    {
        return await _context.Users.Include(u => u.UserRoles).FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _context.Users.Include(u => u.UserRoles).FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<IEnumerable<User>> GetPagedAsync(int page, int pageSize)
    {
        return await _context.Users.Include(u => u.UserRoles)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> CountAsync()
    {
        return await _context.Users.CountAsync();
    }

    public async Task<User> AddAsync(User user)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task UpdateAsync(User user)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(User user)
    {
        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<User>> SearchAsync(string term, int page, int pageSize)
    {
        return await _context.Users.Include(u => u.UserRoles)
            .Where(u => u.FullName.Contains(term) || u.Email.Contains(term))
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task DeleteUserRoleAsync(UserRole userRole)
    {
        _context.UserRoles.Remove(userRole);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteUserRolesAsync(List<UserRole> userRoles)
    {
        _context.UserRoles.RemoveRange(userRoles);
        await _context.SaveChangesAsync();
    }
}


