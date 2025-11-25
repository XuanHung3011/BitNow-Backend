using BitNow_Backend.BLL.IServices;
using BitNow_Backend.Controllers;
using BitNow_Backend.DAL.DTOs;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace BitNow_Backend.Tests.Controllers;

public class UsersControllerTests
{
    private readonly Mock<IUserService> _userServiceMock;
    private readonly Mock<ILogger<UsersController>> _loggerMock;
    private readonly UsersController _controller;

    public UsersControllerTests()
    {
        _userServiceMock = new Mock<IUserService>();
        _loggerMock = new Mock<ILogger<UsersController>>();
        _controller = new UsersController(_userServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetUsers_ReturnsOk()
    {
        // Arrange
        var users = new List<UserResponseDto>
        {
            new UserResponseDto { Id = 1, Email = "user1@example.com", FullName = "User 1" },
            new UserResponseDto { Id = 2, Email = "user2@example.com", FullName = "User 2" }
        };

        _userServiceMock.Setup(x => x.GetAllAsync(1, 10))
            .ReturnsAsync(users);

        // Act
        var result = await _controller.GetUsers(1, 10);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(users);
    }

    [Fact]
    public async Task GetUser_WithValidId_ReturnsOk()
    {
        // Arrange
        var user = new UserResponseDto
        {
            Id = 1,
            Email = "test@example.com",
            FullName = "Test User"
        };

        _userServiceMock.Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(user);

        // Act
        var result = await _controller.GetUser(1);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(user);
    }

    [Fact]
    public async Task GetUser_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        _userServiceMock.Setup(x => x.GetByIdAsync(999))
            .ReturnsAsync((UserResponseDto?)null);

        // Act
        var result = await _controller.GetUser(999);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetUserByEmail_WithValidEmail_ReturnsOk()
    {
        // Arrange
        var user = new UserResponseDto
        {
            Id = 1,
            Email = "test@example.com",
            FullName = "Test User"
        };

        _userServiceMock.Setup(x => x.GetByEmailAsync("test@example.com"))
            .ReturnsAsync(user);

        // Act
        var result = await _controller.GetUserByEmail("test@example.com");

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(user);
    }

    [Fact]
    public async Task CreateUser_WithValidData_ReturnsCreated()
    {
        // Arrange
        var createDto = new UserCreateDto
        {
            Email = "newuser@example.com",
            Password = "Password123",
            FullName = "New User"
        };

        var createdUser = new UserResponseDto
        {
            Id = 1,
            Email = "newuser@example.com",
            FullName = "New User"
        };

        _userServiceMock.Setup(x => x.CreateAsync(createDto))
            .ReturnsAsync(createdUser);

        // Act
        var result = await _controller.CreateUser(createDto);

        // Assert
        result.Result.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = result.Result as CreatedAtActionResult;
        createdResult!.Value.Should().BeEquivalentTo(createdUser);
    }

    [Fact]
    public async Task CreateUser_WithInvalidModel_ReturnsBadRequest()
    {
        // Arrange
        var createDto = new UserCreateDto
        {
            Email = "invalid-email",
            Password = "123",
            FullName = ""
        };

        _controller.ModelState.AddModelError("Email", "Invalid email format");

        // Act
        var result = await _controller.CreateUser(createDto);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task UpdateUser_WithValidData_ReturnsOk()
    {
        // Arrange
        var updateDto = new UserUpdateDto
        {
            FullName = "Updated Name",
            Phone = "1234567890"
        };

        var updatedUser = new UserResponseDto
        {
            Id = 1,
            Email = "test@example.com",
            FullName = "Updated Name",
            Phone = "1234567890"
        };

        _userServiceMock.Setup(x => x.UpdateAsync(1, updateDto))
            .ReturnsAsync(updatedUser);

        // Act
        var result = await _controller.UpdateUser(1, updateDto);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(updatedUser);
    }

    [Fact]
    public async Task UpdateUser_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var updateDto = new UserUpdateDto
        {
            FullName = "Updated Name"
        };

        _userServiceMock.Setup(x => x.UpdateAsync(999, updateDto))
            .ReturnsAsync((UserResponseDto?)null);

        // Act
        var result = await _controller.UpdateUser(999, updateDto);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task ChangePassword_WithValidData_ReturnsOk()
    {
        // Arrange
        var changePasswordDto = new ChangePasswordDto
        {
            CurrentPassword = "OldPassword123",
            NewPassword = "NewPassword123"
        };

        var user = new UserResponseDto
        {
            Id = 1,
            Email = "test@example.com"
        };

        _userServiceMock.Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(user);
        _userServiceMock.Setup(x => x.ValidateCredentialsAsync(user.Email, changePasswordDto.CurrentPassword))
            .ReturnsAsync(true);
        _userServiceMock.Setup(x => x.ChangePasswordAsync(1, changePasswordDto))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.ChangePassword(1, changePasswordDto);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task ChangePassword_WithInvalidCurrentPassword_ReturnsUnauthorized()
    {
        // Arrange
        var changePasswordDto = new ChangePasswordDto
        {
            CurrentPassword = "WrongPassword",
            NewPassword = "NewPassword123"
        };

        var user = new UserResponseDto
        {
            Id = 1,
            Email = "test@example.com"
        };

        _userServiceMock.Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(user);
        _userServiceMock.Setup(x => x.ValidateCredentialsAsync(user.Email, changePasswordDto.CurrentPassword))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.ChangePassword(1, changePasswordDto);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task ActivateUser_WithValidId_ReturnsOk()
    {
        // Arrange
        _userServiceMock.Setup(x => x.ActivateUserAsync(1))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.ActivateUser(1);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task DeactivateUser_WithValidId_ReturnsOk()
    {
        // Arrange
        _userServiceMock.Setup(x => x.DeactivateUserAsync(1))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.DeactivateUser(1);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task AddRole_WithValidData_ReturnsOk()
    {
        // Arrange
        var request = new UsersController.AddRoleRequest { Role = "admin" };

        _userServiceMock.Setup(x => x.AddRoleAsync(1, "admin"))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.AddRole(1, request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task RemoveRole_WithValidData_ReturnsOk()
    {
        // Arrange
        _userServiceMock.Setup(x => x.RemoveRoleAsync(1, "admin"))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.RemoveRole(1, "admin");

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task SearchUsers_WithValidSearchTerm_ReturnsOk()
    {
        // Arrange
        var users = new List<UserResponseDto>
        {
            new UserResponseDto { Id = 1, Email = "test@example.com", FullName = "Test User" }
        };

        _userServiceMock.Setup(x => x.SearchAsync("test", 1, 10))
            .ReturnsAsync(users);

        // Act
        var result = await _controller.SearchUsers("test", 1, 10);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task ValidateCredentials_WithValidData_ReturnsOk()
    {
        // Arrange
        var loginDto = new UserLoginDto
        {
            Email = "test@example.com",
            Password = "Password123"
        };

        _userServiceMock.Setup(x => x.ValidateCredentialsAsync(loginDto.Email, loginDto.Password))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.ValidateCredentials(loginDto);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }
}

