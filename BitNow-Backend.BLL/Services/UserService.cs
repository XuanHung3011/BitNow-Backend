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

    /// <summary>
    /// Lấy thông tin người dùng theo Id và ánh xạ sang UserResponseDto.
    /// Trả về null nếu không tìm thấy.
    /// </summary>
    public async Task<UserResponseDto?> GetByIdAsync(int id)
    {
        var user = await _userRepository.GetByIdAsync(id);

        return user != null ? MapToResponseDto(user) : null;
    }

    /// <summary>
    /// Lấy thông tin người dùng theo Email và ánh xạ sang UserResponseDto.
    /// Trả về null nếu không tìm thấy.
    /// </summary>
    public async Task<UserResponseDto?> GetByEmailAsync(string email)
    {
        var user = await _userRepository.GetByEmailAsync(email);

        return user != null ? MapToResponseDto(user) : null;
    }

    /// <summary>
    /// Lấy danh sách người dùng có phân trang và ánh xạ sang UserResponseDto.
    /// </summary>
    public async Task<IEnumerable<UserResponseDto>> GetAllAsync(int page = 1, int pageSize = 10)
    {
        var users = await _userRepository.GetPagedAsync(page, pageSize);
        return users.Select(MapToResponseDto).ToList();
    }

    /// <summary>
    /// Tạo người dùng mới từ UserCreateDto.
    /// Kiểm tra trùng email, băm mật khẩu bằng BCrypt, trả về UserResponseDto.
    /// </summary>
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

        // Thêm vai trò mặc định buyer (best-effort)
        try
        {
            user.UserRoles.Add(new UserRole { UserId = user.Id, Role = "buyer", CreatedAt = DateTime.UtcNow });
            await _userRepository.UpdateAsync(user);
        }
        catch
        {
            // ignore role add failure here
        }

        return await GetByIdAsync(user.Id) ?? throw new InvalidOperationException("Failed to create user");
    }

    /// <summary>
    /// Cập nhật thông tin người dùng theo Id từ UserUpdateDto.
    /// Chỉ cập nhật các trường có giá trị.
    /// </summary>
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

    /// <summary>
    /// Xoá người dùng theo Id. Trả về true nếu thành công.
    /// Xóa các UserRoles trước khi xóa User để tránh foreign key constraint violation.
    /// </summary>
    public async Task<bool> DeleteAsync(int id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null) return false;

        // Delete user roles first to avoid foreign key constraint issues
        if (user.UserRoles != null && user.UserRoles.Any())
        {
            await _userRepository.DeleteUserRolesAsync(user.UserRoles.ToList());
        }

        await _userRepository.DeleteAsync(user);
        return true;
    }

    /// <summary>
    /// Đổi mật khẩu cho người dùng: xác thực mật khẩu hiện tại bằng BCrypt,
    /// sau đó băm và lưu mật khẩu mới. Trả về true nếu đổi thành công.
    /// </summary>
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

    /// <summary>
    /// Xác thực thông tin đăng nhập: so khớp email, kiểm tra mật khẩu bằng BCrypt.
    /// </summary>
    public async Task<bool> ValidateCredentialsAsync(string email, string password)
    {
        var user = await _userRepository.GetByEmailAsync(email);
        if (user == null) return false;

        return BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
    }

    /// <summary>
    /// Kích hoạt tài khoản người dùng theo Id.
    /// </summary>
    public async Task<bool> ActivateUserAsync(int id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null) return false;

        user.IsActive = true;
        await _userRepository.UpdateAsync(user);
        return true;
    }

    /// <summary>
    /// Vô hiệu hoá tài khoản người dùng theo Id.
    /// </summary>
    public async Task<bool> DeactivateUserAsync(int id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null) return false;

        user.IsActive = false;
        await _userRepository.UpdateAsync(user);
        return true;
    }

    /// <summary>
    /// Tìm kiếm người dùng theo từ khoá với phân trang, trả về danh sách UserResponseDto.
    /// </summary>
    public async Task<IEnumerable<UserResponseDto>> SearchAsync(string searchTerm, int page = 1, int pageSize = 10)
    {
        var users = await _userRepository.SearchAsync(searchTerm, page, pageSize);
        return users.Select(MapToResponseDto).ToList();
    }

    /// <summary>
    /// Ánh xạ entity User sang DTO phản hồi UserResponseDto.
    /// </summary>
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

    public async Task<bool> AddRoleAsync(int userId, string role)
    {
        if (string.IsNullOrWhiteSpace(role)) return false;
        role = role.Trim().ToLowerInvariant();

        var allowed = new HashSet<string>(new[] { "buyer", "seller", "admin" });
        if (!allowed.Contains(role)) return false;

        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null) return false;

        if (user.UserRoles.Any(r => r.Role == role)) return true; // already has role

        user.UserRoles.Add(new UserRole { UserId = user.Id, Role = role, CreatedAt = DateTime.UtcNow });
        await _userRepository.UpdateAsync(user);
        return true;
    }

    public async Task<bool> RemoveRoleAsync(int userId, string role)
    {
        if (string.IsNullOrWhiteSpace(role)) return false;
        role = role.Trim().ToLowerInvariant();

        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null) return false;

        var toRemove = user.UserRoles.FirstOrDefault(r => r.Role == role);
        if (toRemove == null) return true; // Role doesn't exist, consider it already removed

        // Delete the UserRole entity directly from the repository
        await _userRepository.DeleteUserRoleAsync(toRemove);
        return true;
    }
}
