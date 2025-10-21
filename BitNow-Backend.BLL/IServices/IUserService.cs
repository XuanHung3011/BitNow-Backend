using BitNow_Backend.DAL.DTOs;

namespace BitNow_Backend.BLL.IServices;

public interface IUserService
{
    Task<UserResponseDto?> GetByIdAsync(int id);
    Task<UserResponseDto?> GetByEmailAsync(string email);
    Task<IEnumerable<UserResponseDto>> GetAllAsync(int page = 1, int pageSize = 10);
    Task<UserResponseDto> CreateAsync(UserCreateDto userDto);
    Task<UserResponseDto?> UpdateAsync(int id, UserUpdateDto userDto);
    Task<bool> DeleteAsync(int id);
    Task<bool> ChangePasswordAsync(int userId, ChangePasswordDto changePasswordDto);
    Task<bool> ValidateCredentialsAsync(string email, string password);
    Task<bool> ActivateUserAsync(int id);
    Task<bool> DeactivateUserAsync(int id);
    Task<IEnumerable<UserResponseDto>> SearchAsync(string searchTerm, int page = 1, int pageSize = 10);
}
