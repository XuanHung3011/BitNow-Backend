using BitNow_Backend.BLL.IServices;
using Microsoft.AspNetCore.Mvc;

namespace BitNow_Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ContactController : ControllerBase
{
    private readonly IEmailService _emailService;
    private readonly ILogger<ContactController> _logger;
    private const string ADMIN_EMAIL = "xuanhungdz3011@gmail.com";

    public ContactController(IEmailService emailService, ILogger<ContactController> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult> SendContactMessage([FromBody] ContactMessageDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            await _emailService.SendContactEmailAsync(
                toEmail: ADMIN_EMAIL,
                name: dto.Name,
                email: dto.Email,
                subject: dto.Subject,
                category: dto.Category,
                message: dto.Message,
                userId: dto.UserId
            );

            return Ok(new { message = "Đã gửi tin nhắn thành công. Chúng tôi sẽ phản hồi bạn sớm nhất có thể." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending contact message");
            return StatusCode(500, new { message = "Không thể gửi tin nhắn. Vui lòng thử lại sau." });
        }
    }

    public class ContactMessageDto
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public int? UserId { get; set; }
    }
}

