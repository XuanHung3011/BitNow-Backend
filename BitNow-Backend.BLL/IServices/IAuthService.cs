using BitNow_Backend.DAL.DTOs;

namespace BitNow_Backend.BLL.IServices;

public interface IAuthService
{
    Task<UserResponseDto> RegisterAsync(UserCreateDto dto);
    Task<UserResponseDto?> LoginAsync(string email, string password);
    Task<bool> VerifyEmailAsync(string token);
    Task<string> GenerateAndStoreVerificationAsync(int userId, string email);
}

