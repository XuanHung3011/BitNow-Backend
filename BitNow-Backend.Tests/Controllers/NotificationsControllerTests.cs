using BitNow_Backend.BLL.IServices;
using BitNow_Backend.Controllers;
using BitNow_Backend.DAL.DTOs;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace BitNow_Backend.Tests.Controllers;

public class NotificationsControllerTests
{
    private readonly Mock<INotificationService> _notificationServiceMock;
    private readonly Mock<ILogger<NotificationsController>> _loggerMock;
    private readonly NotificationsController _controller;

    public NotificationsControllerTests()
    {
        _notificationServiceMock = new Mock<INotificationService>();
        _loggerMock = new Mock<ILogger<NotificationsController>>();
        _controller = new NotificationsController(_notificationServiceMock.Object, _loggerMock.Object);
    }

    #region GetNotifications

    /// <summary>
    /// Test ID: NOTIF-01
    /// Precondition: User tồn tại trong hệ thống, NotificationService hoạt động bình thường
    /// Input: UserId hợp lệ (1), page hợp lệ (1), pageSize hợp lệ (20)
    /// Condition: Lấy danh sách notifications của user với tham số hợp lệ
    /// Confirmation: HTTP 200 OK, trả về danh sách NotificationResponseDto
    /// Type: Normal
    /// Test Requirement: Kiểm tra chức năng lấy danh sách notifications thành công
    /// </summary>
    [Fact]
    public async Task GetNotifications_WithValidParams_ReturnsOk()
    {
        // Arrange
        var expectedNotifications = new List<NotificationResponseDto>
        {
            new()
            {
                Id = 1,
                UserId = 1,
                Type = "item_pending",
                Message = "Test notification",
                IsRead = false,
                CreatedAt = DateTime.Now
            }
        };

        _notificationServiceMock.Setup(x => x.GetNotificationsByUserIdAsync(1, 1, 20))
            .ReturnsAsync(expectedNotifications);

        // Act
        var result = await _controller.GetNotifications(1, 1, 20);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(expectedNotifications);
    }

    /// <summary>
    /// Test ID: NOTIF-02
    /// Precondition: Service gặp lỗi hệ thống
    /// Input: UserId hợp lệ (1), page hợp lệ (1), pageSize hợp lệ (20)
    /// Condition: Service throw Exception (database error)
    /// Confirmation: HTTP 500 InternalServerError, thông báo Internal server error
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra xử lý exception từ service
    /// </summary>
    [Fact]
    public async Task GetNotifications_WithException_ReturnsInternalServerError()
    {
        // Arrange
        _notificationServiceMock.Setup(x => x.GetNotificationsByUserIdAsync(1, 1, 20))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetNotifications(1, 1, 20);

        // Assert
        result.Result.Should().BeOfType<ObjectResult>();
        var objectResult = result.Result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
    }

    #endregion

    #region GetUnreadNotifications

    /// <summary>
    /// Test ID: NOTIF-03
    /// Precondition: User tồn tại và có notifications chưa đọc, NotificationService hoạt động bình thường
    /// Input: UserId hợp lệ (1), page hợp lệ (1), pageSize hợp lệ (20)
    /// Condition: Lấy danh sách unread notifications của user với tham số hợp lệ
    /// Confirmation: HTTP 200 OK, trả về danh sách NotificationResponseDto với IsRead=false
    /// Type: Normal
    /// Test Requirement: Kiểm tra chức năng lấy danh sách unread notifications thành công
    /// </summary>
    [Fact]
    public async Task GetUnreadNotifications_WithValidParams_ReturnsOk()
    {
        // Arrange
        var expectedNotifications = new List<NotificationResponseDto>
        {
            new()
            {
                Id = 1,
                UserId = 1,
                Type = "item_pending",
                Message = "Unread notification",
                IsRead = false,
                CreatedAt = DateTime.Now
            }
        };

        _notificationServiceMock.Setup(x => x.GetUnreadNotificationsByUserIdAsync(1, 1, 20))
            .ReturnsAsync(expectedNotifications);

        // Act
        var result = await _controller.GetUnreadNotifications(1, 1, 20);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(expectedNotifications);
    }

    /// <summary>
    /// Test ID: NOTIF-04
    /// Precondition: Service gặp lỗi hệ thống
    /// Input: UserId hợp lệ (1), page hợp lệ (1), pageSize hợp lệ (20)
    /// Condition: Service throw Exception (database error)
    /// Confirmation: HTTP 500 InternalServerError, thông báo Internal server error
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra xử lý exception từ service
    /// </summary>
    [Fact]
    public async Task GetUnreadNotifications_WithException_ReturnsInternalServerError()
    {
        // Arrange
        _notificationServiceMock.Setup(x => x.GetUnreadNotificationsByUserIdAsync(1, 1, 20))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetUnreadNotifications(1, 1, 20);

        // Assert
        result.Result.Should().BeOfType<ObjectResult>();
        var objectResult = result.Result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
    }

    #endregion

    #region GetUnreadCount

    /// <summary>
    /// Test ID: NOTIF-05
    /// Precondition: User tồn tại trong hệ thống, NotificationService hoạt động bình thường
    /// Input: UserId hợp lệ (1)
    /// Condition: Lấy số lượng unread notifications của user
    /// Confirmation: HTTP 200 OK, trả về UnreadNotificationCountDto với Count=5
    /// Type: Normal
    /// Test Requirement: Kiểm tra chức năng đếm số lượng unread notifications thành công
    /// </summary>
    [Fact]
    public async Task GetUnreadCount_WithValidUserId_ReturnsOk()
    {
        // Arrange
        _notificationServiceMock.Setup(x => x.GetUnreadCountAsync(1))
            .ReturnsAsync(5);

        // Act
        var result = await _controller.GetUnreadCount(1);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var value = okResult!.Value as UnreadNotificationCountDto;
        value!.Count.Should().Be(5);
    }

    /// <summary>
    /// Test ID: NOTIF-06
    /// Precondition: Service gặp lỗi hệ thống
    /// Input: UserId hợp lệ (1)
    /// Condition: Service throw Exception (database error)
    /// Confirmation: HTTP 500 InternalServerError, thông báo Internal server error
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra xử lý exception từ service
    /// </summary>
    [Fact]
    public async Task GetUnreadCount_WithException_ReturnsInternalServerError()
    {
        // Arrange
        _notificationServiceMock.Setup(x => x.GetUnreadCountAsync(1))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetUnreadCount(1);

        // Assert
        result.Result.Should().BeOfType<ObjectResult>();
        var objectResult = result.Result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
    }

    #endregion

    #region CreateNotification

    /// <summary>
    /// Test ID: NOTIF-07
    /// Precondition: User tồn tại trong hệ thống, NotificationService hoạt động bình thường
    /// Input: CreateNotificationDto hợp lệ (UserId=1, Type="item_pending", Message="Test notification", Link="/test")
    /// Condition: Tạo notification mới với thông tin hợp lệ
    /// Confirmation: HTTP 201 Created, trả về NotificationResponseDto với Id=1
    /// Type: Normal
    /// Test Requirement: Kiểm tra chức năng tạo notification thành công
    /// </summary>
    [Fact]
    public async Task CreateNotification_WithValidDto_ReturnsCreated()
    {
        // Arrange
        var dto = new CreateNotificationDto
        {
            UserId = 1,
            Type = "item_pending",
            Message = "Test notification",
            Link = "/test"
        };

        var expectedNotification = new NotificationResponseDto
        {
            Id = 1,
            UserId = 1,
            Type = "item_pending",
            Message = "Test notification",
            Link = "/test",
            IsRead = false,
            CreatedAt = DateTime.Now
        };

        _notificationServiceMock.Setup(x => x.CreateNotificationAsync(dto))
            .ReturnsAsync(expectedNotification);

        // Act
        var result = await _controller.CreateNotification(dto);

        // Assert
        result.Result.Should().BeOfType<CreatedAtActionResult>();
    }

    /// <summary>
    /// Test ID: NOTIF-08
    /// Precondition: User không tồn tại hoặc dữ liệu không hợp lệ
    /// Input: CreateNotificationDto với UserId không hợp lệ hoặc thiếu thông tin bắt buộc
    /// Condition: Service throw ArgumentException (Invalid user)
    /// Confirmation: HTTP 400 BadRequest, thông báo lỗi từ service
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra xử lý trường hợp dữ liệu không hợp lệ
    /// </summary>
    [Fact]
    public async Task CreateNotification_WithArgumentException_ReturnsBadRequest()
    {
        // Arrange
        var dto = new CreateNotificationDto
        {
            UserId = 1,
            Type = "item_pending",
            Message = "Test notification"
        };

        _notificationServiceMock.Setup(x => x.CreateNotificationAsync(dto))
            .ThrowsAsync(new ArgumentException("Invalid user"));

        // Act
        var result = await _controller.CreateNotification(dto);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    /// <summary>
    /// Test ID: NOTIF-09
    /// Precondition: Service gặp lỗi hệ thống
    /// Input: CreateNotificationDto hợp lệ
    /// Condition: Service throw Exception (database error)
    /// Confirmation: HTTP 500 InternalServerError, thông báo Internal server error
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra xử lý exception từ service
    /// </summary>
    [Fact]
    public async Task CreateNotification_WithException_ReturnsInternalServerError()
    {
        // Arrange
        var dto = new CreateNotificationDto
        {
            UserId = 1,
            Type = "item_pending",
            Message = "Test notification"
        };

        _notificationServiceMock.Setup(x => x.CreateNotificationAsync(dto))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.CreateNotification(dto);

        // Assert
        result.Result.Should().BeOfType<ObjectResult>();
        var objectResult = result.Result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
    }

    #endregion

    #region MarkAsRead

    /// <summary>
    /// Test ID: NOTIF-10
    /// Precondition: Notification tồn tại và thuộc về user, NotificationService hoạt động bình thường
    /// Input: NotificationId hợp lệ (1), UserId hợp lệ (1)
    /// Condition: Đánh dấu notification là đã đọc
    /// Confirmation: HTTP 200 OK, thông báo Notification marked as read
    /// Type: Normal
    /// Test Requirement: Kiểm tra chức năng đánh dấu notification đã đọc thành công
    /// </summary>
    [Fact]
    public async Task MarkAsRead_WithValidParams_ReturnsOk()
    {
        // Arrange
        _notificationServiceMock.Setup(x => x.MarkAsReadAsync(1, 1))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.MarkAsRead(1, 1);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    /// <summary>
    /// Test ID: NOTIF-11
    /// Precondition: Notification không tồn tại hoặc không thuộc về user
    /// Input: NotificationId không tồn tại (1), UserId hợp lệ (1)
    /// Condition: Đánh dấu notification không tồn tại hoặc không thuộc user
    /// Confirmation: HTTP 404 NotFound, thông báo Notification not found or does not belong to user
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra xử lý trường hợp notification không tồn tại
    /// </summary>
    [Fact]
    public async Task MarkAsRead_WhenNotFound_ReturnsNotFound()
    {
        // Arrange
        _notificationServiceMock.Setup(x => x.MarkAsReadAsync(1, 1))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.MarkAsRead(1, 1);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    /// <summary>
    /// Test ID: NOTIF-12
    /// Precondition: Service gặp lỗi hệ thống
    /// Input: NotificationId hợp lệ (1), UserId hợp lệ (1)
    /// Condition: Service throw Exception (database error)
    /// Confirmation: HTTP 500 InternalServerError, thông báo Internal server error
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra xử lý exception từ service
    /// </summary>
    [Fact]
    public async Task MarkAsRead_WithException_ReturnsInternalServerError()
    {
        // Arrange
        _notificationServiceMock.Setup(x => x.MarkAsReadAsync(1, 1))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.MarkAsRead(1, 1);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
    }

    #endregion

    #region MarkAllAsRead

    /// <summary>
    /// Test ID: NOTIF-13
    /// Precondition: User tồn tại trong hệ thống, NotificationService hoạt động bình thường
    /// Input: UserId hợp lệ (1)
    /// Condition: Đánh dấu tất cả notifications của user là đã đọc
    /// Confirmation: HTTP 200 OK, thông báo All notifications marked as read
    /// Type: Normal
    /// Test Requirement: Kiểm tra chức năng đánh dấu tất cả notifications đã đọc thành công
    /// </summary>
    [Fact]
    public async Task MarkAllAsRead_WithValidUserId_ReturnsOk()
    {
        // Arrange
        _notificationServiceMock.Setup(x => x.MarkAllAsReadAsync(1))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.MarkAllAsRead(1);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    /// <summary>
    /// Test ID: NOTIF-14
    /// Precondition: User không tồn tại trong hệ thống
    /// Input: UserId không tồn tại (1)
    /// Condition: Đánh dấu tất cả notifications của user không tồn tại
    /// Confirmation: HTTP 404 NotFound, thông báo User not found
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra xử lý trường hợp user không tồn tại
    /// </summary>
    [Fact]
    public async Task MarkAllAsRead_WhenUserNotFound_ReturnsNotFound()
    {
        // Arrange
        _notificationServiceMock.Setup(x => x.MarkAllAsReadAsync(1))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.MarkAllAsRead(1);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    /// <summary>
    /// Test ID: NOTIF-15
    /// Precondition: Service gặp lỗi hệ thống
    /// Input: UserId hợp lệ (1)
    /// Condition: Service throw Exception (database error)
    /// Confirmation: HTTP 500 InternalServerError, thông báo Internal server error
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra xử lý exception từ service
    /// </summary>
    [Fact]
    public async Task MarkAllAsRead_WithException_ReturnsInternalServerError()
    {
        // Arrange
        _notificationServiceMock.Setup(x => x.MarkAllAsReadAsync(1))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.MarkAllAsRead(1);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
    }

    #endregion

    #region DeleteNotification

    /// <summary>
    /// Test ID: NOTIF-16
    /// Precondition: Notification tồn tại và thuộc về user, NotificationService hoạt động bình thường
    /// Input: NotificationId hợp lệ (1), UserId hợp lệ (1)
    /// Condition: Xóa notification của user
    /// Confirmation: HTTP 200 OK, thông báo Notification deleted
    /// Type: Normal
    /// Test Requirement: Kiểm tra chức năng xóa notification thành công
    /// </summary>
    [Fact]
    public async Task DeleteNotification_WithValidParams_ReturnsOk()
    {
        // Arrange
        _notificationServiceMock.Setup(x => x.DeleteNotificationAsync(1, 1))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteNotification(1, 1);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    /// <summary>
    /// Test ID: NOTIF-17
    /// Precondition: Notification không tồn tại hoặc không thuộc về user
    /// Input: NotificationId không tồn tại (1), UserId hợp lệ (1)
    /// Condition: Xóa notification không tồn tại hoặc không thuộc user
    /// Confirmation: HTTP 404 NotFound, thông báo Notification not found or does not belong to user
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra xử lý trường hợp notification không tồn tại
    /// </summary>
    [Fact]
    public async Task DeleteNotification_WhenNotFound_ReturnsNotFound()
    {
        // Arrange
        _notificationServiceMock.Setup(x => x.DeleteNotificationAsync(1, 1))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.DeleteNotification(1, 1);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    /// <summary>
    /// Test ID: NOTIF-18
    /// Precondition: Service gặp lỗi hệ thống
    /// Input: NotificationId hợp lệ (1), UserId hợp lệ (1)
    /// Condition: Service throw Exception (database error)
    /// Confirmation: HTTP 500 InternalServerError, thông báo Internal server error
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra xử lý exception từ service
    /// </summary>
    [Fact]
    public async Task DeleteNotification_WithException_ReturnsInternalServerError()
    {
        // Arrange
        _notificationServiceMock.Setup(x => x.DeleteNotificationAsync(1, 1))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.DeleteNotification(1, 1);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
    }

    #endregion
}

