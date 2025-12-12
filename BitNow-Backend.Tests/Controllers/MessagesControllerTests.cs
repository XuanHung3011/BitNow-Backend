using BitNow_Backend.BLL.IServices;
using BitNow_Backend.Controllers;
using BitNow_Backend.DAL.DTOs;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using BitNow_Backend.RealTime;

namespace BitNow_Backend.Tests.Controllers;

public class MessagesControllerTests
{
    private readonly Mock<IMessageService> _messageServiceMock;
    private readonly Mock<ILogger<MessagesController>> _loggerMock;
    private readonly Mock<IHubContext<MessageHub>> _hubContextMock;
    private readonly MessagesController _controller;

    public MessagesControllerTests()
    {
        _messageServiceMock = new Mock<IMessageService>();
        _loggerMock = new Mock<ILogger<MessagesController>>();
        _hubContextMock = new Mock<IHubContext<MessageHub>>();
        _controller = new MessagesController(
            _messageServiceMock.Object,
            _loggerMock.Object,
            _hubContextMock.Object);
    }

    #region SendMessage Tests

    /// <summary>
    /// Test ID: MSG-01
    /// Precondition: Sender và Receiver tồn tại, Auction tồn tại (nếu có), MessageService hoạt động bình thường, SignalR HubContext hoạt động
    /// Input: SendMessageRequest hợp lệ (SenderId=1, ReceiverId=2, AuctionId=1, Content="Hello, this is a test message")
    /// Condition: Gửi message giữa 2 users và broadcast qua SignalR
    /// Confirmation: HTTP 200 OK, trả về MessageResponseDto với Id=1, đồng thời broadcast message qua SignalR
    /// Type: Normal
    /// Test Requirement: Kiểm tra chức năng gửi message thành công và broadcast real-time
    /// </summary>
    [Fact]
    public async Task SendMessage_WithValidRequest_ReturnsOk()
    {
        // Arrange
        var request = new SendMessageRequest
        {
            SenderId = 1,
            ReceiverId = 2,
            AuctionId = 1,
            Content = "Hello, this is a test message"
        };

        var expectedMessage = new MessageResponseDto
        {
            Id = 1,
            SenderId = 1,
            SenderName = "Sender User",
            ReceiverId = 2,
            ReceiverName = "Receiver User",
            AuctionId = 1,
            Content = "Hello, this is a test message",
            IsRead = false,
            SentAt = DateTime.UtcNow
        };

        _messageServiceMock.Setup(x => x.SendMessageAsync(request))
            .ReturnsAsync(expectedMessage);

        // Mock HubContext clients
        var mockClients = new Mock<IHubClients>();
        var mockGroup = new Mock<IClientProxy>();
        _hubContextMock.Setup(x => x.Clients).Returns(mockClients.Object);
        _hubContextMock.Setup(x => x.Clients.Group(It.IsAny<string>())).Returns(mockGroup.Object);
        mockGroup.Setup(x => x.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), default))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.SendMessage(request);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(expectedMessage);
        _messageServiceMock.Verify(x => x.SendMessageAsync(request), Times.Once);
    }

    /// <summary>
    /// Test ID: MSG-02
    /// Precondition: ModelState không hợp lệ
    /// Input: SendMessageRequest với Content rỗng ("")
    /// Condition: Gửi message với dữ liệu không hợp lệ theo validation rules
    /// Confirmation: HTTP 400 BadRequest, không gọi SendMessageAsync
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra validation model state trước khi xử lý
    /// </summary>
    [Fact]
    public async Task SendMessage_WithInvalidModel_ReturnsBadRequest()
    {
        // Arrange
        var request = new SendMessageRequest
        {
            SenderId = 1,
            ReceiverId = 2,
            Content = "" // Invalid: empty content
        };

        _controller.ModelState.AddModelError("Content", "Content is required");

        // Act
        var result = await _controller.SendMessage(request);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
        _messageServiceMock.Verify(x => x.SendMessageAsync(It.IsAny<SendMessageRequest>()), Times.Never);
    }

    /// <summary>
    /// Test ID: MSG-03
    /// Precondition: Service trả về null (message không được tạo)
    /// Input: SendMessageRequest hợp lệ
    /// Condition: Service trả về null thay vì MessageResponseDto
    /// Confirmation: HTTP 400 BadRequest, thông báo Failed to send message
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra xử lý trường hợp service trả về null
    /// </summary>
    [Fact]
    public async Task SendMessage_WithNullMessage_ReturnsBadRequest()
    {
        // Arrange
        var request = new SendMessageRequest
        {
            SenderId = 1,
            ReceiverId = 2,
            Content = "Test message"
        };

        _messageServiceMock.Setup(x => x.SendMessageAsync(request))
            .ReturnsAsync((MessageResponseDto?)null);

        // Act
        var result = await _controller.SendMessage(request);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
        _messageServiceMock.Verify(x => x.SendMessageAsync(request), Times.Once);
    }

    /// <summary>
    /// Test ID: MSG-04
    /// Precondition: SenderId = ReceiverId (không hợp lệ)
    /// Input: SendMessageRequest với SenderId=1, ReceiverId=1 (cùng một user)
    /// Condition: Service throw ArgumentException (Cannot send message to yourself)
    /// Confirmation: HTTP 400 BadRequest, thông báo lỗi từ service
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra xử lý trường hợp gửi message cho chính mình
    /// </summary>
    [Fact]
    public async Task SendMessage_WithArgumentException_ReturnsBadRequest()
    {
        // Arrange
        var request = new SendMessageRequest
        {
            SenderId = 1,
            ReceiverId = 1, // Same as sender - should throw ArgumentException
            Content = "Test message"
        };

        _messageServiceMock.Setup(x => x.SendMessageAsync(request))
            .ThrowsAsync(new ArgumentException("Cannot send message to yourself"));

        // Act
        var result = await _controller.SendMessage(request);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result.Result as BadRequestObjectResult;
        badRequestResult!.Value.Should().NotBeNull();
    }

    /// <summary>
    /// Test ID: MSG-05
    /// Precondition: Service gặp lỗi hệ thống
    /// Input: SendMessageRequest hợp lệ
    /// Condition: Service throw Exception (database error)
    /// Confirmation: HTTP 500 InternalServerError, thông báo Internal server error
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra xử lý exception từ service
    /// </summary>
    [Fact]
    public async Task SendMessage_WithException_ReturnsInternalServerError()
    {
        // Arrange
        var request = new SendMessageRequest
        {
            SenderId = 1,
            ReceiverId = 2,
            Content = "Test message"
        };

        _messageServiceMock.Setup(x => x.SendMessageAsync(request))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.SendMessage(request);

        // Assert
        result.Result.Should().BeOfType<ObjectResult>();
        var objectResult = result.Result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
    }

    #endregion

    #region GetConversations Tests

    /// <summary>
    /// Test ID: MSG-06
    /// Precondition: User tồn tại trong hệ thống, có conversations, MessageService hoạt động bình thường
    /// Input: UserId hợp lệ (1)
    /// Condition: Lấy danh sách conversations của user với last message và unread count
    /// Confirmation: HTTP 200 OK, trả về danh sách ConversationDto với OtherUserId, LastMessage, UnreadCount
    /// Type: Normal
    /// Test Requirement: Kiểm tra chức năng lấy danh sách conversations thành công
    /// </summary>
    [Fact]
    public async Task GetConversations_WithValidUserId_ReturnsOk()
    {
        // Arrange
        var userId = 1;
        var expectedConversations = new List<ConversationDto>
        {
            new ConversationDto
            {
                OtherUserId = 2,
                OtherUserName = "User 2",
                LastMessage = "Last message",
                LastMessageTime = DateTime.UtcNow,
                UnreadCount = 2
            },
            new ConversationDto
            {
                OtherUserId = 3,
                OtherUserName = "User 3",
                LastMessage = "Another message",
                LastMessageTime = DateTime.UtcNow.AddHours(-1),
                UnreadCount = 0
            }
        };

        _messageServiceMock.Setup(x => x.GetConversationsAsync(userId))
            .ReturnsAsync(expectedConversations);

        // Act
        var result = await _controller.GetConversations(userId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(expectedConversations);
        _messageServiceMock.Verify(x => x.GetConversationsAsync(userId), Times.Once);
    }

    /// <summary>
    /// Test ID: MSG-07
    /// Precondition: Service gặp lỗi hệ thống
    /// Input: UserId hợp lệ (1)
    /// Condition: Service throw Exception (database error)
    /// Confirmation: HTTP 500 InternalServerError, thông báo Internal server error
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra xử lý exception từ service
    /// </summary>
    [Fact]
    public async Task GetConversations_WithException_ReturnsInternalServerError()
    {
        // Arrange
        var userId = 1;

        _messageServiceMock.Setup(x => x.GetConversationsAsync(userId))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetConversations(userId);

        // Assert
        result.Result.Should().BeOfType<ObjectResult>();
        var objectResult = result.Result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
    }

    #endregion

    #region GetConversation Tests

    /// <summary>
    /// Test ID: MSG-08
    /// Precondition: Cả 2 users tồn tại trong hệ thống, có messages giữa họ, MessageService hoạt động bình thường
    /// Input: UserId1 hợp lệ (1), UserId2 hợp lệ (2), AuctionId = null (không filter theo auction)
    /// Condition: Lấy danh sách messages trong conversation giữa 2 users
    /// Confirmation: HTTP 200 OK, trả về danh sách MessageResponseDto sắp xếp theo thời gian
    /// Type: Normal
    /// Test Requirement: Kiểm tra chức năng lấy conversation messages thành công
    /// </summary>
    [Fact]
    public async Task GetConversation_WithValidParams_ReturnsOk()
    {
        // Arrange
        var userId1 = 1;
        var userId2 = 2;
        var expectedMessages = new List<MessageResponseDto>
        {
            new MessageResponseDto
            {
                Id = 1,
                SenderId = 1,
                ReceiverId = 2,
                Content = "Message 1",
                SentAt = DateTime.UtcNow
            },
            new MessageResponseDto
            {
                Id = 2,
                SenderId = 2,
                ReceiverId = 1,
                Content = "Message 2",
                SentAt = DateTime.UtcNow.AddMinutes(5)
            }
        };

        _messageServiceMock.Setup(x => x.GetConversationAsync(userId1, userId2, null))
            .ReturnsAsync(expectedMessages);

        // Act
        var result = await _controller.GetConversation(userId1, userId2);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(expectedMessages);
        _messageServiceMock.Verify(x => x.GetConversationAsync(userId1, userId2, null), Times.Once);
    }

    /// <summary>
    /// Test ID: MSG-09
    /// Precondition: Cả 2 users tồn tại, Auction tồn tại, có messages liên quan đến auction, MessageService hoạt động bình thường
    /// Input: UserId1 hợp lệ (1), UserId2 hợp lệ (2), AuctionId hợp lệ (5)
    /// Condition: Lấy danh sách messages trong conversation giữa 2 users liên quan đến auction cụ thể
    /// Confirmation: HTTP 200 OK, trả về danh sách MessageResponseDto với AuctionId=5
    /// Type: Normal
    /// Test Requirement: Kiểm tra chức năng lấy conversation messages theo auction thành công
    /// </summary>
    [Fact]
    public async Task GetConversation_WithAuctionId_ReturnsOk()
    {
        // Arrange
        var userId1 = 1;
        var userId2 = 2;
        var auctionId = 5;
        var expectedMessages = new List<MessageResponseDto>
        {
            new MessageResponseDto
            {
                Id = 1,
                SenderId = 1,
                ReceiverId = 2,
                AuctionId = auctionId,
                Content = "Message about auction",
                SentAt = DateTime.UtcNow
            }
        };

        _messageServiceMock.Setup(x => x.GetConversationAsync(userId1, userId2, auctionId))
            .ReturnsAsync(expectedMessages);

        // Act
        var result = await _controller.GetConversation(userId1, userId2, auctionId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(expectedMessages);
        _messageServiceMock.Verify(x => x.GetConversationAsync(userId1, userId2, auctionId), Times.Once);
    }

    /// <summary>
    /// Test ID: MSG-10
    /// Precondition: Service gặp lỗi hệ thống
    /// Input: UserId1 hợp lệ (1), UserId2 hợp lệ (2)
    /// Condition: Service throw Exception (database error)
    /// Confirmation: HTTP 500 InternalServerError, thông báo Internal server error
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra xử lý exception từ service
    /// </summary>
    [Fact]
    public async Task GetConversation_WithException_ReturnsInternalServerError()
    {
        // Arrange
        var userId1 = 1;
        var userId2 = 2;

        _messageServiceMock.Setup(x => x.GetConversationAsync(userId1, userId2, null))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetConversation(userId1, userId2);

        // Assert
        result.Result.Should().BeOfType<ObjectResult>();
        var objectResult = result.Result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
    }

    #endregion

    #region MarkAsRead Tests

    /// <summary>
    /// Test ID: MSG-11
    /// Precondition: Message tồn tại trong hệ thống, MessageService hoạt động bình thường
    /// Input: MessageId hợp lệ (1)
    /// Condition: Đánh dấu message là đã đọc
    /// Confirmation: HTTP 200 OK, thông báo Message marked as read
    /// Type: Normal
    /// Test Requirement: Kiểm tra chức năng đánh dấu message đã đọc thành công
    /// </summary>
    [Fact]
    public async Task MarkAsRead_WithValidId_ReturnsOk()
    {
        // Arrange
        var messageId = 1;

        _messageServiceMock.Setup(x => x.MarkAsReadAsync(messageId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.MarkAsRead(messageId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _messageServiceMock.Verify(x => x.MarkAsReadAsync(messageId), Times.Once);
    }

    /// <summary>
    /// Test ID: MSG-12
    /// Precondition: Message không tồn tại trong hệ thống
    /// Input: MessageId không tồn tại (999)
    /// Condition: Đánh dấu message không tồn tại là đã đọc
    /// Confirmation: HTTP 404 NotFound, thông báo Message not found
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra xử lý trường hợp message không tồn tại
    /// </summary>
    [Fact]
    public async Task MarkAsRead_WithNotFound_ReturnsNotFound()
    {
        // Arrange
        var messageId = 999;

        _messageServiceMock.Setup(x => x.MarkAsReadAsync(messageId))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.MarkAsRead(messageId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        notFoundResult!.Value.Should().NotBeNull();
    }

    /// <summary>
    /// Test ID: MSG-13
    /// Precondition: Service gặp lỗi hệ thống
    /// Input: MessageId hợp lệ (1)
    /// Condition: Service throw Exception (database error)
    /// Confirmation: HTTP 500 InternalServerError, thông báo Internal server error
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra xử lý exception từ service
    /// </summary>
    [Fact]
    public async Task MarkAsRead_WithException_ReturnsInternalServerError()
    {
        // Arrange
        var messageId = 1;

        _messageServiceMock.Setup(x => x.MarkAsReadAsync(messageId))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.MarkAsRead(messageId);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
    }

    #endregion

    #region GetUnreadMessages Tests

    /// <summary>
    /// Test ID: MSG-14
    /// Precondition: User tồn tại trong hệ thống, có unread messages, MessageService hoạt động bình thường
    /// Input: UserId hợp lệ (1)
    /// Condition: Lấy danh sách unread messages của user
    /// Confirmation: HTTP 200 OK, trả về danh sách MessageResponseDto với IsRead=false
    /// Type: Normal
    /// Test Requirement: Kiểm tra chức năng lấy unread messages thành công
    /// </summary>
    [Fact]
    public async Task GetUnreadMessages_WithValidUserId_ReturnsOk()
    {
        // Arrange
        var userId = 1;
        var expectedMessages = new List<MessageResponseDto>
        {
            new MessageResponseDto
            {
                Id = 1,
                SenderId = 2,
                ReceiverId = 1,
                Content = "Unread message 1",
                IsRead = false,
                SentAt = DateTime.UtcNow
            },
            new MessageResponseDto
            {
                Id = 2,
                SenderId = 3,
                ReceiverId = 1,
                Content = "Unread message 2",
                IsRead = false,
                SentAt = DateTime.UtcNow.AddMinutes(10)
            }
        };

        _messageServiceMock.Setup(x => x.GetUnreadMessagesAsync(userId))
            .ReturnsAsync(expectedMessages);

        // Act
        var result = await _controller.GetUnreadMessages(userId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(expectedMessages);
        _messageServiceMock.Verify(x => x.GetUnreadMessagesAsync(userId), Times.Once);
    }

    /// <summary>
    /// Test ID: MSG-15
    /// Precondition: Service gặp lỗi hệ thống
    /// Input: UserId hợp lệ (1)
    /// Condition: Service throw Exception (database error)
    /// Confirmation: HTTP 500 InternalServerError, thông báo Internal server error
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra xử lý exception từ service
    /// </summary>
    [Fact]
    public async Task GetUnreadMessages_WithException_ReturnsInternalServerError()
    {
        // Arrange
        var userId = 1;

        _messageServiceMock.Setup(x => x.GetUnreadMessagesAsync(userId))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetUnreadMessages(userId);

        // Assert
        result.Result.Should().BeOfType<ObjectResult>();
        var objectResult = result.Result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
    }

    #endregion

    #region GetAllMessages Tests

    /// <summary>
    /// Test ID: MSG-16
    /// Precondition: User tồn tại trong hệ thống, có messages (sent và received), MessageService hoạt động bình thường
    /// Input: UserId hợp lệ (1)
    /// Condition: Lấy tất cả messages của user (bao gồm cả sent và received)
    /// Confirmation: HTTP 200 OK, trả về danh sách MessageResponseDto với cả sent và received messages
    /// Type: Normal
    /// Test Requirement: Kiểm tra chức năng lấy tất cả messages của user thành công
    /// </summary>
    [Fact]
    public async Task GetAllMessages_WithValidUserId_ReturnsOk()
    {
        // Arrange
        var userId = 1;
        var expectedMessages = new List<MessageResponseDto>
        {
            new MessageResponseDto
            {
                Id = 1,
                SenderId = 1,
                ReceiverId = 2,
                Content = "Sent message",
                IsRead = true,
                SentAt = DateTime.UtcNow.AddHours(-1)
            },
            new MessageResponseDto
            {
                Id = 2,
                SenderId = 2,
                ReceiverId = 1,
                Content = "Received message",
                IsRead = false,
                SentAt = DateTime.UtcNow
            }
        };

        _messageServiceMock.Setup(x => x.GetAllMessagesByUserIdAsync(userId))
            .ReturnsAsync(expectedMessages);

        // Act
        var result = await _controller.GetAllMessages(userId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(expectedMessages);
        _messageServiceMock.Verify(x => x.GetAllMessagesByUserIdAsync(userId), Times.Once);
    }

    /// <summary>
    /// Test ID: MSG-17
    /// Precondition: Service gặp lỗi hệ thống
    /// Input: UserId hợp lệ (1)
    /// Condition: Service throw Exception (database error)
    /// Confirmation: HTTP 500 InternalServerError, thông báo Internal server error
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra xử lý exception từ service
    /// </summary>
    [Fact]
    public async Task GetAllMessages_WithException_ReturnsInternalServerError()
    {
        // Arrange
        var userId = 1;

        _messageServiceMock.Setup(x => x.GetAllMessagesByUserIdAsync(userId))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetAllMessages(userId);

        // Assert
        result.Result.Should().BeOfType<ObjectResult>();
        var objectResult = result.Result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
    }

    #endregion
}

