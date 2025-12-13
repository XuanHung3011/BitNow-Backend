using BitNow_Backend.BLL.IServices;
using BitNow_Backend.DAL;
using BitNow_Backend.DAL.DTOs;
using BitNow_Backend.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BitNow_Backend.Services;
using System.IO;

namespace BitNow_Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UsersController> _logger;
    private readonly IFileUploadService _fileUploadService;
    private readonly BidNowDbContext _dbContext;

    public UsersController(IUserService userService, ILogger<UsersController> logger, IFileUploadService fileUploadService, BidNowDbContext dbContext)
    {
        _userService = userService;
        _logger = logger;
        _fileUploadService = fileUploadService;
        _dbContext = dbContext;
    }

    private int? GetCurrentUserId()
    {
        // Try to get from header first (custom authentication)
        var userIdHeader = Request.Headers["X-User-Id"].FirstOrDefault();
        if (!string.IsNullOrEmpty(userIdHeader) && int.TryParse(userIdHeader, out var userId))
        {
            return userId;
        }

        // Fallback: try to get from User claims if available
        var userIdClaim = User.FindFirst("userId")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out userId))
            return null;
        return userId;
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
    /// Update user (Admin/Support only, or user updating their own profile)
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<UserResponseDto>> UpdateUser(int id, [FromBody] UserUpdateDto userDto)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
                return Unauthorized();

            // Allow if user is updating their own profile, or if user is admin/support
            var isAdminOrSupport = await RoleHelper.HasAnyRoleAsync(_dbContext, currentUserId, "admin", "support");
            if (currentUserId != id && !isAdminOrSupport)
                return Forbid();

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
    /// Reset user password (Admin/Support only - no current password required)
    /// </summary>
    [HttpPut("{id}/reset-password")]
    public async Task<ActionResult> ResetPassword(int id, [FromBody] ResetPasswordDto resetPasswordDto)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
                return Unauthorized();

            // Check if user is admin or support
            if (!await RoleHelper.HasAnyRoleAsync(_dbContext, currentUserId, "admin", "support"))
                return Forbid();

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Validate new password length
            if (resetPasswordDto.NewPassword.Length < 6)
                return BadRequest(new { message = "Mật khẩu mới phải có ít nhất 6 ký tự" });

            var user = await _userService.GetByIdAsync(id);
            if (user == null)
                return NotFound(new { message = "Không tìm thấy người dùng" });

            // Use ChangePasswordAsync with empty current password (will be handled by service)
            var result = await _userService.ChangePasswordAsync(id, new ChangePasswordDto
            {
                CurrentPassword = "", // Not required for admin/support reset
                NewPassword = resetPasswordDto.NewPassword
            });

            if (!result)
                return BadRequest(new { message = "Không thể đặt lại mật khẩu" });

            return Ok(new { message = "Đặt lại mật khẩu thành công" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting password for user {UserId}", id);
            return StatusCode(500, new { message = "Lỗi server, vui lòng thử lại sau" });
        }
    }

    public class ResetPasswordDto
    {
        public string NewPassword { get; set; } = string.Empty;
    }

    /// <summary>
    /// Generate and send new password via email (Support only)
    /// </summary>
    [HttpPost("{id}/generate-password")]
    public async Task<ActionResult> GenerateAndSendPassword(int id)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
                return Unauthorized();

            // Check if user is support
            if (!await RoleHelper.HasAnyRoleAsync(_dbContext, currentUserId, "support"))
                return Forbid();

            var user = await _userService.GetByIdAsync(id);
            if (user == null)
                return NotFound(new { message = "Không tìm thấy người dùng" });

            var newPassword = await _userService.GenerateAndSendPasswordAsync(id);

            return Ok(new { message = "Đã tạo mật khẩu mới và gửi qua email thành công", password = newPassword });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating and sending password for user {UserId}", id);
            return StatusCode(500, new { message = $"Lỗi server: {ex.Message}" });
        }
    }

    /// <summary>
    /// Change user password (Admin/Support only, or user changing their own password)
    /// </summary>
    [HttpPut("{id}/change-password")]
    public async Task<ActionResult> ChangePassword(int id, [FromBody] ChangePasswordDto changePasswordDto)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
                return Unauthorized();

            // Allow if user is changing their own password, or if user is admin/support
            var isAdminOrSupport = await RoleHelper.HasAnyRoleAsync(_dbContext, currentUserId, "admin", "support");
            if (currentUserId != id && !isAdminOrSupport)
                return Forbid();

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Validate new password length
            if (changePasswordDto.NewPassword.Length < 6)
                return BadRequest(new { message = "Mật khẩu mới phải có ít nhất 6 ký tự" });

            var user = await _userService.GetByIdAsync(id);
            if (user == null)
                return NotFound(new { message = "Không tìm thấy người dùng" });

            // If admin/support is changing password for another user, skip current password verification
            if (isAdminOrSupport && currentUserId != id)
            {
                // Admin/Support can change password without current password
                var result = await _userService.ChangePasswordAsync(id, new ChangePasswordDto
                {
                    CurrentPassword = "", // Empty for admin/support
                    NewPassword = changePasswordDto.NewPassword
                });
                if (!result)
                    return BadRequest(new { message = "Không thể đổi mật khẩu" });
            }
            else
            {
                // User changing their own password - verify current password
                if (string.IsNullOrEmpty(changePasswordDto.CurrentPassword))
                    return BadRequest(new { message = "Mật khẩu hiện tại là bắt buộc" });

                if (!await _userService.ValidateCredentialsAsync(user.Email, changePasswordDto.CurrentPassword))
                    return Unauthorized(new { message = "Mật khẩu hiện tại không đúng" });

                var result = await _userService.ChangePasswordAsync(id, changePasswordDto);
                if (!result)
                    return BadRequest(new { message = "Không thể đổi mật khẩu" });
            }

            return Ok(new { message = "Đổi mật khẩu thành công" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password for user {UserId}", id);
            return StatusCode(500, new { message = "Lỗi server, vui lòng thử lại sau" });
        }
    }

    /// <summary>
    /// Activate user (Admin/Staff only, can only activate buyer/seller)
    /// </summary>
    [HttpPut("{id}/activate")]
    public async Task<ActionResult> ActivateUser(int id)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
                return Unauthorized();

            // Check if user is admin or staff
            if (!await RoleHelper.HasAnyRoleAsync(_dbContext, currentUserId, "admin", "staff"))
                return Forbid();

            // Check if target user is buyer or seller (not admin/staff/support)
            var targetUser = await _dbContext.Users
                .Include(u => u.UserRoles)
                .FirstOrDefaultAsync(u => u.Id == id);
            
            if (targetUser == null)
                return NotFound($"User with ID {id} not found");

            var targetRoles = targetUser.UserRoles.Select(ur => ur.Role.ToLower()).ToList();
            if (targetRoles.Contains("admin") || targetRoles.Contains("staff") || targetRoles.Contains("support"))
                return Forbid("Cannot activate admin, staff, or support users");

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
    /// Deactivate user (Admin/Staff only, can only deactivate buyer/seller)
    /// </summary>
    [HttpPut("{id}/deactivate")]
    public async Task<ActionResult> DeactivateUser(int id)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
                return Unauthorized();

            // Check if user is admin or staff
            if (!await RoleHelper.HasAnyRoleAsync(_dbContext, currentUserId, "admin", "staff"))
                return Forbid();

            // Check if target user is buyer or seller (not admin/staff/support)
            var targetUser = await _dbContext.Users
                .Include(u => u.UserRoles)
                .FirstOrDefaultAsync(u => u.Id == id);
            
            if (targetUser == null)
                return NotFound($"User with ID {id} not found");

            var targetRoles = targetUser.UserRoles.Select(ur => ur.Role.ToLower()).ToList();
            if (targetRoles.Contains("admin") || targetRoles.Contains("staff") || targetRoles.Contains("support"))
                return Forbid("Cannot deactivate admin, staff, or support users");

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
    /// Add a role to user (buyer/seller/admin/staff/support)
    /// - Admin can add any role to any user
    /// - Users can add "seller" role to themselves
    /// </summary>
    [HttpPost("{id}/roles")]
    public async Task<ActionResult> AddRole(int id, [FromBody] AddRoleRequest body)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
                return Unauthorized();

            if (body == null || string.IsNullOrWhiteSpace(body.Role))
                return BadRequest(new { message = "role is required" });

            // Validate role name
            var validRoles = new[] { "buyer", "seller", "admin", "staff", "support" };
            var roleLower = body.Role.ToLower();
            if (!validRoles.Contains(roleLower))
                return BadRequest(new { message = $"Invalid role. Valid roles are: {string.Join(", ", validRoles)}" });

            // Check permissions
            var isAdmin = await RoleHelper.IsAdminAsync(_dbContext, currentUserId);
            
            // If not admin, user can only add "seller" role to themselves
            if (!isAdmin)
            {
                if (id != currentUserId)
                    return Forbid(); // Users can only modify their own roles
                
                if (roleLower != "seller")
                    return Forbid(); // Users can only add "seller" role to themselves
            }

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

    /// <summary>
    /// Upload avatar for user
    /// </summary>
    [HttpPost("{id}/avatar")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<object>> UploadAvatar(int id, IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "File is required" });

            // Validate file type
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
                return BadRequest(new { message = $"File extension {extension} is not allowed. Allowed: {string.Join(", ", allowedExtensions)}" });

            // Validate file size (10MB)
            if (file.Length > 10 * 1024 * 1024)
                return BadRequest(new { message = "File size exceeds maximum allowed size of 10MB" });

            // Get user to use their name for filename
            var user = await _userService.GetByIdAsync(id);
            if (user == null)
                return NotFound(new { message = $"User with ID {id} not found" });

            // Upload file using FileUploadService
            var avatarPath = await _fileUploadService.SaveImageAsync(file, $"user-{user.FullName}", 0);

            // Update user avatar URL
            var updateDto = new UserUpdateDto
            {
                AvatarUrl = avatarPath
            };
            var updatedUser = await _userService.UpdateAsync(id, updateDto);
            if (updatedUser == null)
                return NotFound(new { message = $"User with ID {id} not found" });

            return Ok(new { avatarUrl = avatarPath });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading avatar for user {UserId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}
