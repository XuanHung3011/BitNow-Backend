namespace BitNow_Backend.BLL.IServices;

public interface IEmailService
{
    /// <summary>
    /// Gửi email xác minh tài khoản với token xác minh.
    /// </summary>
    Task SendVerificationEmailAsync(string toEmail, string userName, string verificationToken);
    /// <summary>
    /// Gửi email đặt lại mật khẩu với token đặt lại.
    /// </summary>
    Task SendPasswordResetEmailAsync(string toEmail, string userName, string resetToken);
    /// <summary>
    /// Gửi email với mật khẩu mới được tạo tự động (cho admin/support cấp lại mật khẩu).
    /// </summary>
    Task SendNewPasswordEmailAsync(string toEmail, string userName, string newPassword);
    /// <summary>
    /// Gửi email liên hệ từ người dùng đến admin.
    /// </summary>
    Task SendContactEmailAsync(string toEmail, string name, string email, string subject, string category, string message, int? userId = null);
}
