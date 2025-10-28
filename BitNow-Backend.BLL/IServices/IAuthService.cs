using BitNow_Backend.DAL.DTOs;

namespace BitNow_Backend.BLL.IServices;

public interface IAuthService
{
    Task<UserResponseDto> RegisterAsync(UserCreateDto dto);
    Task<UserResponseDto?> LoginAsync(string email, string password);
    Task<bool> VerifyEmailAsync(string token);
    Task<string> GenerateAndStoreVerificationAsync(int userId, string email);
    Task SendVerificationEmailAsync(string email, int userId, string token);
    Task<bool> RequestPasswordResetAsync(string email);
    Task<bool> ResetPasswordAsync(string token, string newPassword);
}

