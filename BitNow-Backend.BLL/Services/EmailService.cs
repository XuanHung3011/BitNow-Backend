using BitNow_Backend.BLL.IServices;
using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BitNow_Backend.BLL.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Gửi email xác minh tài khoản tới người dùng với đường dẫn chứa token xác minh.
    /// </summary>
    public async Task SendVerificationEmailAsync(string toEmail, string userName, string verificationToken)
    {
        try
        {
            var fromEmail = _configuration["Email:FromEmail"] ?? throw new InvalidOperationException("Email:FromEmail not configured");
            var fromName = _configuration["Email:FromName"] ?? "BidNow";
            var smtpServer = _configuration["Email:SmtpServer"] ?? "smtp.gmail.com";
            var smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "587");
            var username = _configuration["Email:Username"] ?? throw new InvalidOperationException("Email:Username not configured");
            var password = _configuration["Email:Password"] ?? throw new InvalidOperationException("Email:Password not configured");
            var baseUrl = _configuration["Email:BaseUrl"] ?? "https://bitnow.io.vn";

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromName, fromEmail));
            message.To.Add(new MailboxAddress(userName, toEmail));
            message.Subject = "Xác minh tài khoản BidNow";

            var verificationUrl = $"{baseUrl}/verify?token={verificationToken}";
            
            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = $@"
                    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>
                        <div style='text-align: center; margin-bottom: 30px;'>
                            <h1 style='color: #2563eb; margin: 0;'>BidNow</h1>
                            <p style='color: #666; margin: 5px 0;'>Đấu giá thời gian thực</p>
                        </div>
                        
                        <div style='background: #f8fafc; padding: 30px; border-radius: 8px; margin-bottom: 20px;'>
                            <h2 style='color: #1f2937; margin-top: 0;'>Chào mừng {userName}!</h2>
                            <p style='color: #4b5563; line-height: 1.6;'>
                                Cảm ơn bạn đã đăng ký tài khoản BidNow. Để hoàn tất quá trình đăng ký, 
                                vui lòng xác minh địa chỉ email của bạn bằng cách nhấn vào nút bên dưới.
                            </p>
                            
                            <div style='text-align: center; margin: 30px 0;'>
                                <a href='{verificationUrl}' 
                                   style='background: #2563eb; color: white; padding: 12px 30px; 
                                          text-decoration: none; border-radius: 6px; 
                                          display: inline-block; font-weight: bold;'>
                                    Xác minh Email
                                </a>
                            </div>
                            
                            <p style='color: #6b7280; font-size: 14px; margin-bottom: 0;'>
                                Nếu nút không hoạt động, bạn có thể copy và paste link này vào trình duyệt:
                            </p>
                            <p style='color: #2563eb; font-size: 12px; word-break: break-all; margin: 5px 0 0 0;'>
                                {verificationUrl}
                            </p>
                        </div>
                        
                        <div style='text-align: center; color: #9ca3af; font-size: 12px;'>
                            <p>Email này được gửi tự động, vui lòng không trả lời.</p>
                            <p>© 2024 BidNow. Tất cả quyền được bảo lưu.</p>
                        </div>
                    </div>"
            };

            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(smtpServer, smtpPort, MailKit.Security.SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(username, password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation($"Verification email sent to {toEmail}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to send verification email to {toEmail}");
            throw;
        }
    }

    /// <summary>
    /// Gửi email đặt lại mật khẩu tới người dùng với đường dẫn chứa token đặt lại.
    /// </summary>
    public async Task SendPasswordResetEmailAsync(string toEmail, string userName, string resetToken)
    {
        try
        {
            var fromEmail = _configuration["Email:FromEmail"] ?? throw new InvalidOperationException("Email:FromEmail not configured");
            var fromName = _configuration["Email:FromName"] ?? "BidNow";
            var smtpServer = _configuration["Email:SmtpServer"] ?? "smtp.gmail.com";
            var smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "587");
            var username = _configuration["Email:Username"] ?? throw new InvalidOperationException("Email:Username not configured");
            var password = _configuration["Email:Password"] ?? throw new InvalidOperationException("Email:Password not configured");
            var baseUrl = _configuration["Email:BaseUrl"] ?? "https://bitnow.io.vn";

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromName, fromEmail));
            message.To.Add(new MailboxAddress(userName, toEmail));
            message.Subject = "Đặt lại mật khẩu BidNow";

            var resetUrl = $"{baseUrl}/reset-password?token={resetToken}";

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = $@"
                    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>
                        <h2 style='color: #1f2937;'>Yêu cầu đặt lại mật khẩu</h2>
                        <p style='color: #4b5563;'>Chúng tôi nhận được yêu cầu đặt lại mật khẩu cho tài khoản BidNow của bạn.</p>
                        <div style='text-align: center; margin: 30px 0;'>
                            <a href='{resetUrl}' style='background: #2563eb; color: white; padding: 12px 30px; text-decoration: none; border-radius: 6px; display: inline-block; font-weight: bold;'>Đặt lại mật khẩu</a>
                        </div>
                        <p style='color: #6b7280; font-size: 14px;'>Nếu bạn không yêu cầu, hãy bỏ qua email này.</p>
                    </div>"
            };

            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(smtpServer, smtpPort, MailKit.Security.SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(username, password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation($"Password reset email sent to {toEmail}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to send reset email to {toEmail}");
            throw;
        }
    }

    /// <summary>
    /// Gửi email với mật khẩu mới được tạo tự động (cho admin/support cấp lại mật khẩu).
    /// </summary>
    public async Task SendNewPasswordEmailAsync(string toEmail, string userName, string newPassword)
    {
        try
        {
            var fromEmail = _configuration["Email:FromEmail"] ?? throw new InvalidOperationException("Email:FromEmail not configured");
            var fromName = _configuration["Email:FromName"] ?? "BidNow";
            var smtpServer = _configuration["Email:SmtpServer"] ?? "smtp.gmail.com";
            var smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "587");
            var username = _configuration["Email:Username"] ?? throw new InvalidOperationException("Email:Username not configured");
            var password = _configuration["Email:Password"] ?? throw new InvalidOperationException("Email:Password not configured");

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromName, fromEmail));
            message.To.Add(new MailboxAddress(userName, toEmail));
            message.Subject = "Mật khẩu mới cho tài khoản BidNow";

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = $@"
                    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>
                        <div style='text-align: center; margin-bottom: 30px;'>
                            <h1 style='color: #2563eb; margin: 0;'>BidNow</h1>
                            <p style='color: #666; margin: 5px 0;'>Đấu giá thời gian thực</p>
                        </div>
                        
                        <div style='background: #f8fafc; padding: 30px; border-radius: 8px; margin-bottom: 20px;'>
                            <h2 style='color: #1f2937; margin-top: 0;'>Chào {userName}!</h2>
                            <p style='color: #4b5563; line-height: 1.6;'>
                                Mật khẩu mới cho tài khoản BidNow của bạn đã được tạo. 
                                Vui lòng sử dụng mật khẩu bên dưới để đăng nhập.
                            </p>
                            
                            <div style='background: #ffffff; border: 2px solid #2563eb; border-radius: 8px; padding: 20px; margin: 20px 0; text-align: center;'>
                                <p style='color: #6b7280; font-size: 14px; margin: 0 0 10px 0;'>Mật khẩu mới của bạn:</p>
                                <p style='color: #1f2937; font-size: 24px; font-weight: bold; font-family: monospace; letter-spacing: 2px; margin: 0;'>{newPassword}</p>
                            </div>
                            
                            <div style='background: #fef3c7; border-left: 4px solid #f59e0b; padding: 15px; margin: 20px 0; border-radius: 4px;'>
                                <p style='color: #92400e; margin: 0; font-size: 14px;'>
                                    <strong>⚠️ Lưu ý quan trọng:</strong> Vui lòng đổi mật khẩu này ngay sau khi đăng nhập để bảo mật tài khoản của bạn.
                                </p>
                            </div>
                            
                            <p style='color: #6b7280; font-size: 14px; margin-top: 20px;'>
                                Nếu bạn không yêu cầu mật khẩu mới, vui lòng liên hệ với bộ phận hỗ trợ ngay lập tức.
                            </p>
                        </div>
                        
                        <div style='text-align: center; color: #9ca3af; font-size: 12px;'>
                            <p>Email này được gửi tự động, vui lòng không trả lời.</p>
                            <p>© 2024 BidNow. Tất cả quyền được bảo lưu.</p>
                        </div>
                    </div>"
            };

            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(smtpServer, smtpPort, MailKit.Security.SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(username, password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation($"New password email sent to {toEmail}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to send new password email to {toEmail}");
            throw;
        }
    }

    /// <summary>
    /// Gửi email liên hệ từ người dùng đến admin.
    /// </summary>
    public async Task SendContactEmailAsync(string toEmail, string name, string email, string subject, string category, string message, int? userId = null)
    {
        try
        {
            var fromEmail = _configuration["Email:FromEmail"] ?? throw new InvalidOperationException("Email:FromEmail not configured");
            var fromName = _configuration["Email:FromName"] ?? "BidNow";
            var smtpServer = _configuration["Email:SmtpServer"] ?? "smtp.gmail.com";
            var smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "587");
            var username = _configuration["Email:Username"] ?? throw new InvalidOperationException("Email:Username not configured");
            var password = _configuration["Email:Password"] ?? throw new InvalidOperationException("Email:Password not configured");

            var messageObj = new MimeMessage();
            messageObj.From.Add(new MailboxAddress(fromName, fromEmail));
            messageObj.To.Add(new MailboxAddress("Admin", toEmail));
            messageObj.Subject = $"[Liên hệ BidNow] {subject}";

            var categoryLabel = category switch
            {
                "support" => "Hỗ trợ kỹ thuật",
                "account" => "Vấn đề tài khoản",
                "payment" => "Thanh toán",
                "auction" => "Phiên đấu giá",
                "report" => "Báo cáo vi phạm",
                "other" => "Khác",
                _ => category
            };

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = $@"
                    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>
                        <div style='text-align: center; margin-bottom: 30px;'>
                            <h1 style='color: #2563eb; margin: 0;'>BidNow</h1>
                            <p style='color: #666; margin: 5px 0;'>Đấu giá thời gian thực</p>
                        </div>
                        
                        <div style='background: #f8fafc; padding: 30px; border-radius: 8px; margin-bottom: 20px;'>
                            <h2 style='color: #1f2937; margin-top: 0;'>Tin nhắn liên hệ mới</h2>
                            
                            <div style='background: #ffffff; padding: 20px; border-radius: 6px; margin: 20px 0;'>
                                <table style='width: 100%; border-collapse: collapse;'>
                                    <tr>
                                        <td style='padding: 8px 0; color: #6b7280; font-weight: bold; width: 120px;'>Họ và tên:</td>
                                        <td style='padding: 8px 0; color: #1f2937;'>{name}</td>
                                    </tr>
                                    <tr>
                                        <td style='padding: 8px 0; color: #6b7280; font-weight: bold;'>Email:</td>
                                        <td style='padding: 8px 0; color: #1f2937;'>{email}</td>
                                    </tr>
                                    <tr>
                                        <td style='padding: 8px 0; color: #6b7280; font-weight: bold;'>Danh mục:</td>
                                        <td style='padding: 8px 0; color: #1f2937;'>{categoryLabel}</td>
                                    </tr>
                                    {(userId.HasValue ? $@"
                                    <tr>
                                        <td style='padding: 8px 0; color: #6b7280; font-weight: bold;'>User ID:</td>
                                        <td style='padding: 8px 0; color: #1f2937;'>{userId.Value}</td>
                                    </tr>
                                    " : @"
                                    <tr>
                                        <td style='padding: 8px 0; color: #6b7280; font-weight: bold;'>Trạng thái:</td>
                                        <td style='padding: 8px 0; color: #f59e0b;'>Khách (chưa đăng nhập)</td>
                                    </tr>
                                    ")}
                                    <tr>
                                        <td style='padding: 8px 0; color: #6b7280; font-weight: bold; vertical-align: top;'>Nội dung:</td>
                                        <td style='padding: 8px 0; color: #1f2937; white-space: pre-wrap;'>{message}</td>
                                    </tr>
                                </table>
                            </div>
                        </div>
                        
                        <div style='text-align: center; color: #9ca3af; font-size: 12px;'>
                            <p>Email này được gửi tự động từ form liên hệ BidNow.</p>
                            <p>© 2024 BidNow. Tất cả quyền được bảo lưu.</p>
                        </div>
                    </div>"
            };

            messageObj.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(smtpServer, smtpPort, MailKit.Security.SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(username, password);
            await client.SendAsync(messageObj);
            await client.DisconnectAsync(true);

            _logger.LogInformation($"Contact email sent to {toEmail} from {name} ({email})");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to send contact email to {toEmail}");
            throw;
        }
    }
}
