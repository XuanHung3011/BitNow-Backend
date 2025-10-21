using System.ComponentModel.DataAnnotations;

namespace BitNow_Backend.DAL.DTOs;

public class UserCreateDto
{
    [Required]
    [EmailAddress]
    [MaxLength(255)]
    public string Email { get; set; } = null!;

    [Required]
    [MinLength(6)]
    [MaxLength(100)]
    public string Password { get; set; } = null!;

    [Required]
    [MaxLength(255)]
    public string FullName { get; set; } = null!;

    [Phone]
    [MaxLength(20)]
    public string? Phone { get; set; }

    [Url]
    [MaxLength(500)]
    public string? AvatarUrl { get; set; }
}

public class UserUpdateDto
{
    [MaxLength(255)]
    public string? FullName { get; set; }

    [Phone]
    [MaxLength(20)]
    public string? Phone { get; set; }

    [Url]
    [MaxLength(500)]
    public string? AvatarUrl { get; set; }
}

public class UserResponseDto
{
    public int Id { get; set; }
    public string Email { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string? Phone { get; set; }
    public string? AvatarUrl { get; set; }
    public decimal? ReputationScore { get; set; }
    public int? TotalRatings { get; set; }
    public int? TotalSales { get; set; }
    public int? TotalPurchases { get; set; }
    public bool? IsActive { get; set; }
    public DateTime? CreatedAt { get; set; }
    public List<string> Roles { get; set; } = new();
}

public class UserLoginDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = null!;

    [Required]
    public string Password { get; set; } = null!;
}

public class ChangePasswordDto
{
    [Required]
    public string CurrentPassword { get; set; } = null!;

    [Required]
    [MinLength(6)]
    public string NewPassword { get; set; } = null!;
}
