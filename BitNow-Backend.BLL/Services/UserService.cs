using BitNow_Backend.DAL.DTOs;
using BitNow_Backend.DAL.Models;
using BitNow_Backend.DAL.IRepositories;

namespace BitNow_Backend.BLL.Services;

public class UserService : BitNow_Backend.BLL.IServices.IUserService
{
    private readonly IUserRepository _userRepository;

    public UserService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<UserResponseDto?> GetByIdAsync(int id)
    {
        var user = await _userRepository.GetByIdAsync(id);

        return user != null ? MapToResponseDto(user) : null;
    }

    public async Task<UserResponseDto?> GetByEmailAsync(string email)
    {
        var user = await _userRepository.GetByEmailAsync(email);

        return user != null ? MapToResponseDto(user) : null;
    }

    public async Task<IEnumerable<UserResponseDto>> GetAllAsync(int page = 1, int pageSize = 10)
    {
        var users = await _userRepository.GetPagedAsync(page, pageSize);
        return users.Select(MapToResponseDto).ToList();
    }

    public async Task<UserResponseDto> CreateAsync(UserCreateDto userDto)
    {
        // Check if email already exists
        if ((await _userRepository.GetByEmailAsync(userDto.Email)) != null)
        {
            throw new InvalidOperationException("Email already exists");
        }

        var user = new User
        {
            Email = userDto.Email,
            // Use BCrypt for consistency with AuthService
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(userDto.Password),
            FullName = userDto.FullName,
            Phone = userDto.Phone,
            AvatarUrl = userDto.AvatarUrl,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            ReputationScore = 0.00m,
            TotalRatings = 0,
            TotalSales = 0,
            TotalPurchases = 0
        };

        user = await _userRepository.AddAsync(user);

        // Add default role
        // Note: Role assignment would go via a RoleRepository (not implemented here)

        return await GetByIdAsync(user.Id) ?? throw new InvalidOperationException("Failed to create user");
    }

    public async Task<UserResponseDto?> UpdateAsync(int id, UserUpdateDto userDto)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null) return null;

        if (!string.IsNullOrEmpty(userDto.FullName))
            user.FullName = userDto.FullName;
        
        if (userDto.Phone != null)
            user.Phone = userDto.Phone;
        
        if (userDto.AvatarUrl != null)
            user.AvatarUrl = userDto.AvatarUrl;

        await _userRepository.UpdateAsync(user);
        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null) return false;

        await _userRepository.DeleteAsync(user);
        return true;
    }

    public async Task<bool> ChangePasswordAsync(int userId, ChangePasswordDto changePasswordDto)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null) return false;

        // Verify current password with BCrypt
        if (!BCrypt.Net.BCrypt.Verify(changePasswordDto.CurrentPassword, user.PasswordHash))
            return false;

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(changePasswordDto.NewPassword);
        await _userRepository.UpdateAsync(user);
        return true;
    }

    public async Task<bool> ValidateCredentialsAsync(string email, string password)
    {
        var user = await _userRepository.GetByEmailAsync(email);
        if (user == null) return false;

        return BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
    }

    public async Task<bool> ActivateUserAsync(int id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null) return false;

        user.IsActive = true;
        await _userRepository.UpdateAsync(user);
        return true;
    }

    public async Task<bool> DeactivateUserAsync(int id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null) return false;

        user.IsActive = false;
        await _userRepository.UpdateAsync(user);
        return true;
    }

    public async Task<IEnumerable<UserResponseDto>> SearchAsync(string searchTerm, int page = 1, int pageSize = 10)
    {
        var users = await _userRepository.SearchAsync(searchTerm, page, pageSize);
        return users.Select(MapToResponseDto).ToList();
    }

    private static UserResponseDto MapToResponseDto(User user)
    {
        return new UserResponseDto
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            Phone = user.Phone,
            AvatarUrl = user.AvatarUrl,
            ReputationScore = user.ReputationScore,
            TotalRatings = user.TotalRatings,
            TotalSales = user.TotalSales,
            TotalPurchases = user.TotalPurchases,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            Roles = user.UserRoles?.Select(r => r.Role).ToList() ?? new List<string>()
        };
    }

    // Deprecated simple hash helpers removed in favor of BCrypt
}
