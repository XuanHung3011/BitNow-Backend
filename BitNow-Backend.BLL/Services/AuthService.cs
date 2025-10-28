using BitNow_Backend.BLL.IServices;
using BitNow_Backend.DAL.DTOs;
using BitNow_Backend.DAL.IRepositories;
using BitNow_Backend.DAL.Models;

namespace BitNow_Backend.BLL.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IEmailVerificationRepository _verificationRepository;
    private readonly IEmailService _emailService;

    public AuthService(IUserRepository userRepository, IEmailVerificationRepository verificationRepository, IEmailService emailService)
    {
        _userRepository = userRepository;
        _verificationRepository = verificationRepository;
        _emailService = emailService;
    }

    public async Task<UserResponseDto> RegisterAsync(UserCreateDto dto)
    {
        var existing = await _userRepository.GetByEmailAsync(dto.Email);
        if (existing != null)
        {
            // If user exists but not verified yet, resend verification email and return existing user
            if (existing.IsActive != true)
            {
                var resendToken = await GenerateAndStoreVerificationAsync(existing.Id, existing.Email);
                try
                {
                    await _emailService.SendVerificationEmailAsync(existing.Email, existing.FullName, resendToken);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to resend verification email: {ex.Message}");
                }

                return Map(existing);
            }

            // Active user already exists
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

        // Generate verification token and send email (fire-and-forget)
        var token = await GenerateAndStoreVerificationAsync(user.Id, user.Email);
        _ = Task.Run(async () =>
        {
            try
            {
                await _emailService.SendVerificationEmailAsync(user.Email, user.FullName, token);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send verification email: {ex.Message}");
            }
        });

        return Map(user);
    }

    public async Task<UserResponseDto?> LoginAsync(string email, string password)
    {
        var user = await _userRepository.GetByEmailAsync(email);
        if (user == null) throw new InvalidOperationException("User not found");

        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash)) throw new InvalidOperationException("Invalid password");

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

    public async Task<bool> RequestPasswordResetAsync(string email)
    {
        var user = await _userRepository.GetByEmailAsync(email);
        if (user == null) return false;

        var token = await GenerateAndStoreVerificationAsync(user.Id, user.Email);
        _ = Task.Run(async () =>
        {
            try
            {
                await _emailService.SendPasswordResetEmailAsync(user.Email, user.FullName, token);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send reset email: {ex.Message}");
            }
        });
        return true;
    }

    public async Task<bool> ResetPasswordAsync(string token, string newPassword)
    {
        var record = await _verificationRepository.GetByTokenAsync(token);
        if (record == null) return false;
        if (record.IsUsed) return false;
        if (record.ExpiresAt < DateTime.UtcNow) return false;

        var user = await _userRepository.GetByEmailAsync(record.Email);
        if (user == null) return false;

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
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

    public async Task SendVerificationEmailAsync(string email, int userId, string token)
    {
        // Get user info for email
        var user = await _userRepository.GetByIdAsync(userId);
        var userName = user?.FullName ?? "User";
        
        await _emailService.SendVerificationEmailAsync(email, userName, token);
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


