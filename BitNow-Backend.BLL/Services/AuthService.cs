using BitNow_Backend.BLL.IServices;
using BitNow_Backend.DAL.DTOs;
using BitNow_Backend.DAL.IRepositories;
using BitNow_Backend.DAL.Models;

namespace BitNow_Backend.BLL.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IEmailVerificationRepository _verificationRepository;

    public AuthService(IUserRepository userRepository, IEmailVerificationRepository verificationRepository)
    {
        _userRepository = userRepository;
        _verificationRepository = verificationRepository;
    }

    public async Task<UserResponseDto> RegisterAsync(UserCreateDto dto)
    {
        var existing = await _userRepository.GetByEmailAsync(dto.Email);
        if (existing != null)
        {
            throw new InvalidOperationException("Email already exists");
        }

        var user = new User
        {
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            FullName = dto.FullName,
            Phone = dto.Phone,
            AvatarUrl = dto.AvatarUrl,
            IsActive = false, // require verification
            CreatedAt = DateTime.UtcNow,
            ReputationScore = 0.00m,
            TotalRatings = 0,
            TotalSales = 0,
            TotalPurchases = 0
        };

        user = await _userRepository.AddAsync(user);

        await GenerateAndStoreVerificationAsync(user.Id, user.Email);

        return Map(user);
    }

    public async Task<UserResponseDto?> LoginAsync(string email, string password)
    {
        var user = await _userRepository.GetByEmailAsync(email);
        if (user == null) return null;

        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash)) return null;

        if (user.IsActive != true) throw new InvalidOperationException("Email not verified");

        return Map(user);
    }

    public async Task<bool> VerifyEmailAsync(string token)
    {
        var record = await _verificationRepository.GetByTokenAsync(token);
        if (record == null) return false;
        if (record.IsUsed) return false;
        if (record.ExpiresAt < DateTime.UtcNow) return false;

        var user = await _userRepository.GetByEmailAsync(record.Email);
        if (user == null) return false;

        user.IsActive = true;
        await _userRepository.UpdateAsync(user);

        await _verificationRepository.MarkUsedAsync(record);
        return true;
    }

    public async Task<string> GenerateAndStoreVerificationAsync(int userId, string email)
    {
        var token = Convert.ToBase64String(Guid.NewGuid().ToByteArray())
            .Replace("/", "_")
            .Replace("+", "-");

        var verification = new EmailVerification
        {
            UserId = userId,
            Email = email,
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddHours(24),
            IsUsed = false
        };

        await _verificationRepository.AddAsync(verification);
        return token;
    }

    private static UserResponseDto Map(User user)
    {
        return new UserResponseDto
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            Phone = user.Phone,
            AvatarUrl = user.AvatarUrl,
            ReputationScore = user.ReputationScore,
            TotalRatings = user.TotalRatings,
            TotalSales = user.TotalSales,
            TotalPurchases = user.TotalPurchases,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            Roles = user.UserRoles?.Select(r => r.Role).ToList() ?? new List<string>()
        };
    }
}


