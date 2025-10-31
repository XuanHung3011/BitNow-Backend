using BitNow_Backend.DAL.DTOs;

namespace BitNow_Backend.BLL.IServices;

public interface IAuthService
{
    /// <summary>
    /// Đăng ký tài khoản mới và gửi email xác minh.
    /// </summary>
    Task<UserResponseDto> RegisterAsync(UserCreateDto dto);
    /// <summary>
    /// Đăng nhập với email và mật khẩu.
    /// </summary>
    Task<UserResponseDto?> LoginAsync(string email, string password);
    /// <summary>
    /// Xác minh email từ token.
    /// </summary>
    Task<bool> VerifyEmailAsync(string token);
    /// <summary>
    /// Sinh và lưu token xác minh cho user/email.
    /// </summary>
    Task<string> GenerateAndStoreVerificationAsync(int userId, string email);
    /// <summary>
    /// Gửi email xác minh với token đã có.
    /// </summary>
    Task SendVerificationEmailAsync(string email, int userId, string token);
    /// <summary>
    /// Gửi email đặt lại mật khẩu (nếu email tồn tại).
    /// </summary>
    Task<bool> RequestPasswordResetAsync(string email);
    /// <summary>
    /// Đặt lại mật khẩu từ token.
    /// </summary>
    Task<bool> ResetPasswordAsync(string token, string newPassword);
}

