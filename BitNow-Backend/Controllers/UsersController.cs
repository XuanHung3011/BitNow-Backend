using BitNow_Backend.BLL.IServices;
using BitNow_Backend.DAL.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace BitNow_Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IUserService userService, ILogger<UsersController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// Get all users with pagination
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserResponseDto>>> GetUsers(
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 10)
    {
        try
        {
            var users = await _userService.GetAllAsync(page, pageSize);
            return Ok(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get user by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<UserResponseDto>> GetUser(int id)
    {
        try
        {
            var user = await _userService.GetByIdAsync(id);
            if (user == null)
                return NotFound($"User with ID {id} not found");

            return Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user {UserId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get user by email
    /// </summary>
    [HttpGet("email/{email}")]
    public async Task<ActionResult<UserResponseDto>> GetUserByEmail(string email)
    {
        try
        {
            var user = await _userService.GetByEmailAsync(email);
            if (user == null)
                return NotFound($"User with email {email} not found");

            return Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user by email {Email}", email);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Create new user
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<UserResponseDto>> CreateUser([FromBody] UserCreateDto userDto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _userService.CreateAsync(userDto);
            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Update user
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<UserResponseDto>> UpdateUser(int id, [FromBody] UserUpdateDto userDto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _userService.UpdateAsync(id, userDto);
            if (user == null)
                return NotFound($"User with ID {id} not found");

            return Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {UserId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Change user password
    /// </summary>
    [HttpPut("{id}/change-password")]
    public async Task<ActionResult> ChangePassword(int id, [FromBody] ChangePasswordDto changePasswordDto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Validate new password length
            if (changePasswordDto.NewPassword.Length < 6)
                return BadRequest(new { message = "Mật khẩu mới phải có ít nhất 6 ký tự" });

            var user = await _userService.GetByIdAsync(id);
            if (user == null)
                return NotFound(new { message = "Không tìm thấy người dùng" });

            // Verify current password
            if (!await _userService.ValidateCredentialsAsync(user.Email, changePasswordDto.CurrentPassword))
                return Unauthorized(new { message = "Mật khẩu hiện tại không đúng" });

            var result = await _userService.ChangePasswordAsync(id, changePasswordDto);
            if (!result)
                return BadRequest(new { message = "Không thể đổi mật khẩu" });

            return Ok(new { message = "Đổi mật khẩu thành công" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password for user {UserId}", id);
            return StatusCode(500, new { message = "Lỗi server, vui lòng thử lại sau" });
        }
    }

    /// <summary>
    /// Activate user
    /// </summary>
    [HttpPut("{id}/activate")]
    public async Task<ActionResult> ActivateUser(int id)
    {
        try
        {
            var result = await _userService.ActivateUserAsync(id);
            if (!result)
                return NotFound($"User with ID {id} not found");

            return Ok(new { message = "User activated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating user {UserId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Deactivate user
    /// </summary>
    [HttpPut("{id}/deactivate")]
    public async Task<ActionResult> DeactivateUser(int id)
    {
        try
        {
            var result = await _userService.DeactivateUserAsync(id);
            if (!result)
                return NotFound($"User with ID {id} not found");

            return Ok(new { message = "User deactivated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating user {UserId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    public class AddRoleRequest { public string Role { get; set; } = string.Empty; }

    /// <summary>
    /// Add a role to user (buyer/seller/admin)
    /// </summary>
    [HttpPost("{id}/roles")]
    public async Task<ActionResult> AddRole(int id, [FromBody] AddRoleRequest body)
    {
        try
        {
            if (body == null || string.IsNullOrWhiteSpace(body.Role))
                return BadRequest(new { message = "role is required" });

            var ok = await _userService.AddRoleAsync(id, body.Role);
            if (!ok) return BadRequest(new { message = "cannot add role" });
            return Ok(new { message = "role added" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding role for user {UserId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Remove a role from user (buyer/seller/admin)
    /// </summary>
    [HttpDelete("{id}/roles/{role}")]
    public async Task<ActionResult> RemoveRole(int id, string role)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(role)) return BadRequest(new { message = "role is required" });
            var ok = await _userService.RemoveRoleAsync(id, role);
            if (!ok) return BadRequest(new { message = "cannot remove role" });
            return Ok(new { message = "role removed" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing role for user {UserId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Search users
    /// </summary>
    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<UserResponseDto>>> SearchUsers(
        [FromQuery] string searchTerm,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return BadRequest("Search term is required");

            var users = await _userService.SearchAsync(searchTerm, page, pageSize);
            return Ok(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching users with term {SearchTerm}", searchTerm);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Validate user credentials
    /// </summary>
    [HttpPost("validate-credentials")]
    public async Task<ActionResult> ValidateCredentials([FromBody] UserLoginDto loginDto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var isValid = await _userService.ValidateCredentialsAsync(loginDto.Email, loginDto.Password);
            return Ok(new { isValid });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating credentials for {Email}", loginDto.Email);
            return StatusCode(500, "Internal server error");
        }
    }
}
