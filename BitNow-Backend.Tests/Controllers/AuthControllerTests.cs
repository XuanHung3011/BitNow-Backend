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

