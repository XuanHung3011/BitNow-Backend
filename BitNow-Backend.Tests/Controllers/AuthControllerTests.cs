using BitNow_Backend.BLL.IServices;
using BitNow_Backend.Controllers;
using BitNow_Backend.DAL.DTOs;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace BitNow_Backend.Tests.Controllers;

public class AuthControllerTests
{
    private readonly Mock<IAuthService> _authServiceMock;
    private readonly Mock<ILogger<AuthController>> _loggerMock;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _authServiceMock = new Mock<IAuthService>();
        _loggerMock = new Mock<ILogger<AuthController>>();
        _controller = new AuthController(_authServiceMock.Object, _loggerMock.Object);
    }

    /// <summary>
    /// Test ID: AUTH-01
    /// Precondition: Email chưa tồn tại trong hệ thống, AuthService hoạt động bình thường
    /// Input: Email hợp lệ (test@example.com), Password hợp lệ (Password123), FullName hợp lệ (Test User)
    /// Condition: Đăng ký user mới với thông tin hợp lệ
    /// Confirmation: HTTP 200 OK, trả về UserResponseDto với Id, Email, FullName đúng
    /// Type: Normal
    /// Test Requirement: Kiểm tra chức năng đăng ký user mới thành công
    /// </summary>
    [Fact]
    public async Task Register_WithValidData_ReturnsOk()
    {
        // Arrange
        var userDto = new UserCreateDto
        {
            Email = "test@example.com",
            Password = "Password123",
            FullName = "Test User"
        };

        var expectedUser = new UserResponseDto
        {
            Id = 1,
            Email = "test@example.com",
            FullName = "Test User"
        };

        _authServiceMock.Setup(x => x.RegisterAsync(userDto))
            .ReturnsAsync(expectedUser);

        // Act
        var result = await _controller.Register(userDto);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(expectedUser);
        _authServiceMock.Verify(x => x.RegisterAsync(userDto), Times.Once);
    }

    /// <summary>
    /// Test ID: AUTH-02
    /// Precondition: ModelState không hợp lệ
    /// Input: Email không hợp lệ (invalid-email), Password quá ngắn (123), FullName rỗng ("")
    /// Condition: Đăng ký với dữ liệu không hợp lệ theo validation rules
    /// Confirmation: HTTP 400 BadRequest, không gọi RegisterAsync
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra validation model state trước khi xử lý
    /// </summary>
    [Fact]
    public async Task Register_WithInvalidModel_ReturnsBadRequest()
    {
        // Arrange
        var userDto = new UserCreateDto
        {
            Email = "invalid-email",
            Password = "123",
            FullName = ""
        };

        _controller.ModelState.AddModelError("Email", "Invalid email format");

        // Act
        var result = await _controller.Register(userDto);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
        _authServiceMock.Verify(x => x.RegisterAsync(It.IsAny<UserCreateDto>()), Times.Never);
    }

    /// <summary>
    /// Test ID: AUTH-03
    /// Precondition: Email đã tồn tại trong hệ thống
    /// Input: Email đã tồn tại (existing@example.com), Password hợp lệ, FullName hợp lệ
    /// Condition: Đăng ký với email đã được sử dụng
    /// Confirmation: HTTP 409 Conflict, thông báo email đã tồn tại
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra xử lý trường hợp email trùng lặp
    /// </summary>
    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsConflict()
    {
        // Arrange
        var userDto = new UserCreateDto
        {
            Email = "existing@example.com",
            Password = "Password123",
            FullName = "Test User"
        };

        _authServiceMock.Setup(x => x.RegisterAsync(userDto))
            .ThrowsAsync(new InvalidOperationException("Email already exists"));

        // Act
        var result = await _controller.Register(userDto);

        // Assert
        result.Result.Should().BeOfType<ConflictObjectResult>();
    }

    /// <summary>
    /// Test ID: AUTH-04
    /// Precondition: User tồn tại trong hệ thống, email đã được verify, mật khẩu đúng
    /// Input: Email hợp lệ (test@example.com), Password đúng (Password123)
    /// Condition: Đăng nhập với thông tin xác thực hợp lệ
    /// Confirmation: HTTP 200 OK, trả về UserResponseDto với thông tin user
    /// Type: Normal
    /// Test Requirement: Kiểm tra chức năng đăng nhập thành công
    /// </summary>
    [Fact]
    public async Task Login_WithValidCredentials_ReturnsOk()
    {
        // Arrange
        var loginRequest = new AuthController.LoginRequest
        {
            Email = "test@example.com",
            Password = "Password123"
        };

        var expectedUser = new UserResponseDto
        {
            Id = 1,
            Email = "test@example.com",
            FullName = "Test User"
        };

        _authServiceMock.Setup(x => x.LoginAsync(loginRequest.Email, loginRequest.Password))
            .ReturnsAsync(expectedUser);

        // Act
        var result = await _controller.Login(loginRequest);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(expectedUser);
    }

    /// <summary>
    /// Test ID: AUTH-05
    /// Precondition: User tồn tại trong hệ thống
    /// Input: Email hợp lệ (test@example.com), Password sai (WrongPassword)
    /// Condition: Đăng nhập với mật khẩu không đúng
    /// Confirmation: HTTP 401 Unauthorized, thông báo mật khẩu không đúng
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra xử lý trường hợp mật khẩu sai
    /// </summary>
    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var loginRequest = new AuthController.LoginRequest
        {
            Email = "test@example.com",
            Password = "WrongPassword"
        };

        _authServiceMock.Setup(x => x.LoginAsync(loginRequest.Email, loginRequest.Password))
            .ThrowsAsync(new InvalidOperationException("Invalid password"));

        // Act
        var result = await _controller.Login(loginRequest);

        // Assert
        result.Result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    /// <summary>
    /// Test ID: AUTH-06
    /// Precondition: User không tồn tại trong hệ thống
    /// Input: Email không tồn tại (nonexistent@example.com), Password bất kỳ
    /// Condition: Đăng nhập với email chưa được đăng ký
    /// Confirmation: HTTP 404 NotFound, thông báo tài khoản chưa được đăng ký
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra xử lý trường hợp user không tồn tại
    /// </summary>
    [Fact]
    public async Task Login_WithNonExistentUser_ReturnsNotFound()
    {
        // Arrange
        var loginRequest = new AuthController.LoginRequest
        {
            Email = "nonexistent@example.com",
            Password = "Password123"
        };

        _authServiceMock.Setup(x => x.LoginAsync(loginRequest.Email, loginRequest.Password))
            .ThrowsAsync(new InvalidOperationException("User not found"));

        // Act
        var result = await _controller.Login(loginRequest);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    /// <summary>
    /// Test ID: AUTH-07
    /// Precondition: Token verify email hợp lệ và chưa hết hạn
    /// Input: Token hợp lệ (valid-token)
    /// Condition: Xác minh email với token hợp lệ
    /// Confirmation: HTTP 200 OK, thông báo email verified successfully
    /// Type: Normal
    /// Test Requirement: Kiểm tra chức năng xác minh email thành công
    /// </summary>
    [Fact]
    public async Task Verify_WithValidToken_ReturnsOk()
    {
        // Arrange
        var token = "valid-token";
        _authServiceMock.Setup(x => x.VerifyEmailAsync(token))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.Verify(token);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _authServiceMock.Verify(x => x.VerifyEmailAsync(token), Times.Once);
    }

    /// <summary>
    /// Test ID: AUTH-08
    /// Precondition: Token verify email không hợp lệ hoặc đã hết hạn
    /// Input: Token không hợp lệ (invalid-token) hoặc token đã hết hạn
    /// Condition: Xác minh email với token không hợp lệ
    /// Confirmation: HTTP 400 BadRequest, thông báo invalid or expired token
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra xử lý trường hợp token không hợp lệ
    /// </summary>
    [Fact]
    public async Task Verify_WithInvalidToken_ReturnsBadRequest()
    {
        // Arrange
        var token = "invalid-token";
        _authServiceMock.Setup(x => x.VerifyEmailAsync(token))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.Verify(token);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    /// <summary>
    /// Test ID: AUTH-09
    /// Precondition: Email tồn tại trong hệ thống, AuthService hoạt động bình thường
    /// Input: Email hợp lệ và tồn tại (test@example.com)
    /// Condition: Yêu cầu reset password với email hợp lệ
    /// Confirmation: HTTP 200 OK, gửi email reset password thành công
    /// Type: Normal
    /// Test Requirement: Kiểm tra chức năng quên mật khẩu thành công
    /// </summary>
    [Fact]
    public async Task ForgotPassword_WithValidEmail_ReturnsOk()
    {
        // Arrange
        var request = new AuthController.ForgotPasswordRequest
        {
            Email = "test@example.com"
        };

        _authServiceMock.Setup(x => x.RequestPasswordResetAsync(request.Email))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.ForgotPassword(request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    /// <summary>
    /// Test ID: AUTH-10
    /// Precondition: Email không tồn tại trong hệ thống
    /// Input: Email không tồn tại (nonexistent@example.com)
    /// Condition: Yêu cầu reset password với email chưa đăng ký
    /// Confirmation: HTTP 404 NotFound, thông báo email chưa được đăng ký
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra xử lý trường hợp email không tồn tại khi quên mật khẩu
    /// </summary>
    [Fact]
    public async Task ForgotPassword_WithNonExistentEmail_ReturnsNotFound()
    {
        // Arrange
        var request = new AuthController.ForgotPasswordRequest
        {
            Email = "nonexistent@example.com"
        };

        _authServiceMock.Setup(x => x.RequestPasswordResetAsync(request.Email))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.ForgotPassword(request);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    /// <summary>
    /// Test ID: AUTH-11
    /// Precondition: Token reset password hợp lệ và chưa hết hạn, mật khẩu mới hợp lệ
    /// Input: Token hợp lệ (valid-token), mật khẩu mới hợp lệ (NewPassword123)
    /// Condition: Reset password với token và mật khẩu mới hợp lệ
    /// Confirmation: HTTP 200 OK, reset password thành công
    /// Type: Normal
    /// Test Requirement: Kiểm tra chức năng reset password thành công
    /// </summary>
    [Fact]
    public async Task ResetPassword_WithValidToken_ReturnsOk()
    {
        // Arrange
        var request = new AuthController.ResetPasswordRequest
        {
            Token = "valid-token",
            NewPassword = "NewPassword123"
        };

        _authServiceMock.Setup(x => x.ResetPasswordAsync(request.Token, request.NewPassword))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.ResetPassword(request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    /// <summary>
    /// Test ID: AUTH-12
    /// Precondition: Token reset password không hợp lệ hoặc đã hết hạn
    /// Input: Token không hợp lệ (invalid-token), mật khẩu mới hợp lệ
    /// Condition: Reset password với token không hợp lệ
    /// Confirmation: HTTP 400 BadRequest, thông báo token không hợp lệ
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra xử lý trường hợp token reset password không hợp lệ
    /// </summary>
    [Fact]
    public async Task ResetPassword_WithInvalidToken_ReturnsBadRequest()
    {
        // Arrange
        var request = new AuthController.ResetPasswordRequest
        {
            Token = "invalid-token",
            NewPassword = "NewPassword123"
        };

        _authServiceMock.Setup(x => x.ResetPasswordAsync(request.Token, request.NewPassword))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.ResetPassword(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }
}

