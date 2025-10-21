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
            if (user == null) return Unauthorized(new { message = "Invalid credentials" });
            return Ok(user);
        }
        catch (InvalidOperationException ex)
        {
            return Forbid(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login error");
            return StatusCode(500, "Internal server error");
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

    public class ResendRequest { public int UserId { get; set; } public string Email { get; set; } = null!; }

    [HttpPost("resend-verification")]
    public async Task<ActionResult> Resend([FromBody] ResendRequest dto)
    {
        try
        {
            var token = await _authService.GenerateAndStoreVerificationAsync(dto.UserId, dto.Email);
            return Ok(new { token });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Resend verification error");
            return StatusCode(500, "Internal server error");
        }
    }
}


