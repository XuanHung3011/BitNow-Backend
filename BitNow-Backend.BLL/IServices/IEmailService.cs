namespace BitNow_Backend.BLL.IServices;

public interface IEmailService
{
    Task SendVerificationEmailAsync(string toEmail, string userName, string verificationToken);
    Task SendPasswordResetEmailAsync(string toEmail, string userName, string resetToken);
}
