using BitNow_Backend.BLL.IServices;
using BitNow_Backend.Controllers;
using BitNow_Backend.DAL;
using BitNow_Backend.DAL.DTOs;
using BitNow_Backend.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace BitNow_Backend.Tests.Controllers;

public class UsersControllerTests
{
    private readonly Mock<IUserService> _userServiceMock;
    private readonly Mock<ILogger<UsersController>> _loggerMock;
    private readonly Mock<IFileUploadService> _fileUploadServiceMock;
    private readonly UsersController _controller;

    public UsersControllerTests()
    {
        _userServiceMock = new Mock<IUserService>();
        _loggerMock = new Mock<ILogger<UsersController>>();
        _fileUploadServiceMock = new Mock<IFileUploadService>();
        var dbContextMock = new Mock<BidNowDbContext>();
        _controller = new UsersController(_userServiceMock.Object, _loggerMock.Object, _fileUploadServiceMock.Object, dbContextMock.Object);
    }

    /// <summary>
    /// Test ID: USER-01
    /// Precondition: UserService hoạt động bình thường, có users trong hệ thống
    /// Input: page hợp lệ (1), pageSize hợp lệ (10)
    /// Condition: Lấy danh sách users có phân trang
    /// Confirmation: HTTP 200 OK, trả về danh sách UserResponseDto
    /// Type: Normal
    /// Test Requirement: Kiểm tra chức năng lấy danh sách users thành công
    /// </summary>
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

    /// <summary>
    /// Test ID: USER-02
    /// Precondition: User tồn tại trong hệ thống, UserService hoạt động bình thường
    /// Input: UserId hợp lệ (1)
    /// Condition: Lấy thông tin user theo ID hợp lệ
    /// Confirmation: HTTP 200 OK, trả về UserResponseDto với Id=1, Email="test@example.com"
    /// Type: Normal
    /// Test Requirement: Kiểm tra chức năng lấy user theo ID thành công
    /// </summary>
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

    /// <summary>
    /// Test ID: USER-03
    /// Precondition: User không tồn tại trong hệ thống
    /// Input: UserId không tồn tại (999)
    /// Condition: Lấy thông tin user với ID không tồn tại
    /// Confirmation: HTTP 404 NotFound, thông báo User not found
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra xử lý trường hợp user không tồn tại
    /// </summary>
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

    /// <summary>
    /// Test ID: USER-04
    /// Precondition: User tồn tại trong hệ thống với email hợp lệ, UserService hoạt động bình thường
    /// Input: Email hợp lệ ("test@example.com")
    /// Condition: Lấy thông tin user theo email hợp lệ
    /// Confirmation: HTTP 200 OK, trả về UserResponseDto với Email="test@example.com"
    /// Type: Normal
    /// Test Requirement: Kiểm tra chức năng lấy user theo email thành công
    /// </summary>
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

    /// <summary>
    /// Test ID: USER-05
    /// Precondition: Email chưa tồn tại trong hệ thống, UserService hoạt động bình thường
    /// Input: UserCreateDto hợp lệ (Email="newuser@example.com", Password="Password123", FullName="New User")
    /// Condition: Tạo user mới với thông tin hợp lệ
    /// Confirmation: HTTP 201 Created, trả về UserResponseDto với Id=1, Email="newuser@example.com"
    /// Type: Normal
    /// Test Requirement: Kiểm tra chức năng tạo user thành công
    /// </summary>
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

    /// <summary>
    /// Test ID: USER-06
    /// Precondition: ModelState không hợp lệ
    /// Input: UserCreateDto với Email không hợp lệ, Password quá ngắn, FullName rỗng
    /// Condition: Tạo user với dữ liệu không hợp lệ theo validation rules
    /// Confirmation: HTTP 400 BadRequest, không gọi CreateAsync
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra validation model state trước khi xử lý
    /// </summary>
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

    /// <summary>
    /// Test ID: USER-07
    /// Precondition: User tồn tại trong hệ thống, UserService hoạt động bình thường
    /// Input: UserId hợp lệ (1), UserUpdateDto hợp lệ (FullName="Updated Name", Phone="1234567890")
    /// Condition: Cập nhật thông tin user với dữ liệu hợp lệ
    /// Confirmation: HTTP 200 OK, trả về UserResponseDto đã được cập nhật
    /// Type: Normal
    /// Test Requirement: Kiểm tra chức năng cập nhật user thành công
    /// </summary>
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

    /// <summary>
    /// Test ID: USER-08
    /// Precondition: User không tồn tại trong hệ thống
    /// Input: UserId không tồn tại (999), UserUpdateDto hợp lệ
    /// Condition: Cập nhật user với ID không tồn tại
    /// Confirmation: HTTP 404 NotFound, thông báo User not found
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra xử lý trường hợp user không tồn tại khi cập nhật
    /// </summary>
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

    /// <summary>
    /// Test ID: USER-09
    /// Precondition: User tồn tại trong hệ thống, CurrentPassword đúng, UserService hoạt động bình thường
    /// Input: UserId hợp lệ (1), ChangePasswordDto hợp lệ (CurrentPassword="OldPassword123", NewPassword="NewPassword123")
    /// Condition: Đổi mật khẩu với current password đúng
    /// Confirmation: HTTP 200 OK, thông báo Password changed successfully
    /// Type: Normal
    /// Test Requirement: Kiểm tra chức năng đổi mật khẩu thành công
    /// </summary>
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

    /// <summary>
    /// Test ID: USER-10
    /// Precondition: User tồn tại nhưng CurrentPassword sai
    /// Input: UserId hợp lệ (1), ChangePasswordDto với CurrentPassword sai ("WrongPassword")
    /// Condition: Đổi mật khẩu với current password sai
    /// Confirmation: HTTP 401 Unauthorized, thông báo Current password is incorrect
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra xử lý trường hợp current password sai
    /// </summary>
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

    /// <summary>
    /// Test ID: USER-11
    /// Precondition: User tồn tại trong hệ thống, UserService hoạt động bình thường
    /// Input: UserId hợp lệ (1)
    /// Condition: Kích hoạt user (activate user account)
    /// Confirmation: HTTP 200 OK, thông báo User activated successfully
    /// Type: Normal
    /// Test Requirement: Kiểm tra chức năng kích hoạt user thành công
    /// </summary>
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

    /// <summary>
    /// Test ID: USER-12
    /// Precondition: User tồn tại trong hệ thống, UserService hoạt động bình thường
    /// Input: UserId hợp lệ (1)
    /// Condition: Vô hiệu hóa user (deactivate user account)
    /// Confirmation: HTTP 200 OK, thông báo User deactivated successfully
    /// Type: Normal
    /// Test Requirement: Kiểm tra chức năng vô hiệu hóa user thành công
    /// </summary>
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

    /// <summary>
    /// Test ID: USER-13
    /// Precondition: User tồn tại trong hệ thống, Role hợp lệ, UserService hoạt động bình thường
    /// Input: UserId hợp lệ (1), AddRoleRequest hợp lệ (Role="admin")
    /// Condition: Thêm role cho user
    /// Confirmation: HTTP 200 OK, thông báo Role added successfully
    /// Type: Normal
    /// Test Requirement: Kiểm tra chức năng thêm role cho user thành công
    /// </summary>
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

    /// <summary>
    /// Test ID: USER-14
    /// Precondition: User tồn tại và có role, UserService hoạt động bình thường
    /// Input: UserId hợp lệ (1), Role hợp lệ ("admin")
    /// Condition: Xóa role khỏi user
    /// Confirmation: HTTP 200 OK, thông báo Role removed successfully
    /// Type: Normal
    /// Test Requirement: Kiểm tra chức năng xóa role khỏi user thành công
    /// </summary>
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

    /// <summary>
    /// Test ID: USER-15
    /// Precondition: UserService hoạt động bình thường, có users khớp với search term
    /// Input: searchTerm hợp lệ ("test"), page hợp lệ (1), pageSize hợp lệ (10)
    /// Condition: Tìm kiếm users với search term và phân trang
    /// Confirmation: HTTP 200 OK, trả về danh sách UserResponseDto khớp với search term
    /// Type: Normal
    /// Test Requirement: Kiểm tra chức năng tìm kiếm users thành công
    /// </summary>
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

    /// <summary>
    /// Test ID: USER-16
    /// Precondition: User tồn tại trong hệ thống, Email và Password đúng, UserService hoạt động bình thường
    /// Input: UserLoginDto hợp lệ (Email="test@example.com", Password="Password123")
    /// Condition: Xác thực credentials của user
    /// Confirmation: HTTP 200 OK, trả về true nếu credentials hợp lệ
    /// Type: Normal
    /// Test Requirement: Kiểm tra chức năng xác thực credentials thành công
    /// </summary>
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

