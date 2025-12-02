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

