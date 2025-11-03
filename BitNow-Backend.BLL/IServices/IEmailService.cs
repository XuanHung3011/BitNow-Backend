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
}
