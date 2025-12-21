using BitNow_Backend.BLL.IServices;
using BitNow_Backend.DAL.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace BitNow_Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<ActionResult<UserResponseDto>> Register([FromBody] UserCreateDto dto)
    {
        try
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var user = await _authService.RegisterAsync(dto);
            return Ok(user);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Register error");
            return StatusCode(500, "Internal server error");
        }
    }

    public class LoginRequest { public string Email { get; set; } = null!; public string Password { get; set; } = null!; }

    [HttpPost("login")]
    public async Task<ActionResult<UserResponseDto>> Login([FromBody] LoginRequest dto)
    {
        try 
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var user = await _authService.LoginAsync(dto.Email, dto.Password);
            return Ok(user);
        }
        catch (InvalidOperationException ex)
        {
            if (ex.Message == "User not found")
                return NotFound("Tài khoản chưa được đăng ký");
            else if (ex.Message == "Invalid password")
                return Unauthorized("Mật khẩu không đúng");
            else if (ex.Message == "Email not verified")
                return Forbid("Email chưa được xác minh");
            else if (ex.Message == "Account deactivated")
                return Forbid("Tài khoản đã bị khóa");
            else
                return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login error: {Message}, StackTrace: {StackTrace}", ex.Message, ex.StackTrace);
            return StatusCode(500, new { message = $"Internal server error: {ex.Message}" });
        }
    }

    [HttpPost("verify")]
    public async Task<ActionResult> Verify([FromQuery] string token)
    {
        try
        {
            var ok = await _authService.VerifyEmailAsync(token);
            if (!ok) return BadRequest(new { message = "Invalid or expired token" });
            return Ok(new { message = "Email verified successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Verify error");
            return StatusCode(500, "Internal server error");
        }
    }

    public class ForgotPasswordRequest { public string Email { get; set; } = null!; }

    [HttpPost("forgot-password")]
    public async Task<ActionResult> ForgotPassword([FromBody] ForgotPasswordRequest dto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.Email))
                return BadRequest(new { message = "Email is required" });

            var exists = await _authService.RequestPasswordResetAsync(dto.Email);
            if (!exists)
            {
                return NotFound(new { message = "Email chưa được đăng ký" });
            }

            return Ok(new { message = "Đã gửi link đặt lại mật khẩu đến email của bạn" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Forgot password error");
            return StatusCode(500, "Internal server error");
        }
    }

    public class ResetPasswordRequest { public string Token { get; set; } = null!; public string NewPassword { get; set; } = null!; }

    [HttpPost("reset-password")]
    public async Task<ActionResult> ResetPassword([FromBody] ResetPasswordRequest dto)
    {
        try
        {
            var ok = await _authService.ResetPasswordAsync(dto.Token, dto.NewPassword);
            if (!ok) return BadRequest(new { message = "Invalid or expired token" });
            return Ok(new { message = "Password reset successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Reset password error");
            return StatusCode(500, "Internal server error");
        }
    }
    public class ResendRequest { public int UserId { get; set; } public string Email { get; set; } = null!; }

    [HttpPost("resend-verification")]
    public async Task<ActionResult> Resend([FromBody] ResendRequest dto)
    {
        try
        {
            if (dto.UserId <= 0 || string.IsNullOrWhiteSpace(dto.Email))
            {
                return BadRequest(new { message = "UserId and Email are required" });
            }

            // Validate user exists and email matches
            var user = await _authService.GetUserByIdAsync(dto.UserId);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            if (!string.Equals(user.Email, dto.Email, StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { message = "Email does not match the user account" });
            }

            // Check if user is already verified
            if (user.IsActive == true)
            {
                return BadRequest(new { message = "Email is already verified" });
            }

            var token = await _authService.GenerateAndStoreVerificationAsync(dto.UserId, dto.Email);
            
            // Send email with the new token (fire-and-forget to avoid blocking)
            _ = Task.Run(async () =>
            {
                try
                {
                    await _authService.SendVerificationEmailAsync(dto.Email, dto.UserId, token);
                    _logger.LogInformation("Verification email sent successfully to {Email} for UserId {UserId}", dto.Email, dto.UserId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send verification email to {Email} for UserId {UserId}", dto.Email, dto.UserId);
                }
            });
            
            return Ok(new { message = "Verification email will be sent shortly" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Resend verification error: {Message}, StackTrace: {StackTrace}", ex.Message, ex.StackTrace);
            return StatusCode(500, new { message = $"Internal server error: {ex.Message}" });
        }
    }

}


