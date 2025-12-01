using BitNow_Backend.BLL.IServices;
using BitNow_Backend.Controllers;
using BitNow_Backend.DAL.DTOs;
using BitNow_Backend.RealTime;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;

namespace BitNow_Backend.Tests.Controllers;

public class AuctionMessagesControllerTests
{
    private readonly Mock<IAuctionChatService> _auctionChatServiceMock;
    private readonly Mock<ILogger<AuctionMessagesController>> _loggerMock;
    private readonly Mock<IHubContext<MessageHub>> _hubContextMock;
    private readonly AuctionMessagesController _controller;

    public AuctionMessagesControllerTests()
    {
        _auctionChatServiceMock = new Mock<IAuctionChatService>();
        _loggerMock = new Mock<ILogger<AuctionMessagesController>>();
        _hubContextMock = new Mock<IHubContext<MessageHub>>();
        _controller = new AuctionMessagesController(
            _auctionChatServiceMock.Object,
            _loggerMock.Object,
            _hubContextMock.Object);
    }

    #region GetMessages

    [Fact]
    public async Task GetMessages_WithValidRequest_ReturnsOk()
    {
        // Arrange
        var auctionId = 1;
        var limit = 50;
        var viewerId = 10;

        var expectedMessages = new List<AuctionChatMessageDto>
        {
            new()
            {
                Id = 1,
                Alias = "User1",
                Content = "Hello",
                SentAt = DateTime.UtcNow,
                IsMine = true
            },
            new()
            {
                Id = 2,
                Alias = "User2",
                Content = "Hi",
                SentAt = DateTime.UtcNow.AddMinutes(1),
                IsMine = false
            }
        };

        _auctionChatServiceMock.Setup(x => x.GetMessagesAsync(auctionId, limit, viewerId))
            .ReturnsAsync(expectedMessages);

        // Act
        var result = await _controller.GetMessages(auctionId, limit, viewerId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(expectedMessages);
        _auctionChatServiceMock.Verify(x => x.GetMessagesAsync(auctionId, limit, viewerId), Times.Once);
    }

    [Fact]
    public async Task GetMessages_WithArgumentException_ReturnsBadRequest()
    {
        // Arrange
        var auctionId = 1;

        _auctionChatServiceMock.Setup(x => x.GetMessagesAsync(auctionId, It.IsAny<int>(), It.IsAny<int?>()))
            .ThrowsAsync(new ArgumentException("Invalid auction ID"));

        // Act
        var result = await _controller.GetMessages(auctionId);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetMessages_WithException_ReturnsInternalServerError()
    {
        // Arrange
        var auctionId = 1;

        _auctionChatServiceMock.Setup(x => x.GetMessagesAsync(auctionId, It.IsAny<int>(), It.IsAny<int?>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetMessages(auctionId);

        // Assert
        result.Result.Should().BeOfType<ObjectResult>();
        var objectResult = result.Result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
    }

    #endregion

    #region CreateMessage

    [Fact]
    public async Task CreateMessage_WithValidRequest_ReturnsOkAndBroadcasts()
    {
        // Arrange
        var request = new CreateAuctionChatMessageRequest
        {
            AuctionId = 1,
            SenderId = 10,
            Content = "New message"
        };

        var createdMessage = new AuctionChatMessageDto
        {
            Id = 1,
            Alias = "User10",
            Content = request.Content,
            SentAt = DateTime.UtcNow,
            IsMine = true
        };

        _auctionChatServiceMock.Setup(x => x.AddMessageAsync(request))
            .ReturnsAsync(createdMessage);

        // Mock HubContext clients
        var mockClients = new Mock<IHubClients>();
        var mockGroup = new Mock<IClientProxy>();
        _hubContextMock.Setup(x => x.Clients).Returns(mockClients.Object);
        _hubContextMock.Setup(x => x.Clients.Group(It.IsAny<string>())).Returns(mockGroup.Object);
        mockGroup.Setup(x => x.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), default))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.CreateMessage(request);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(createdMessage);

        _auctionChatServiceMock.Verify(x => x.AddMessageAsync(request), Times.Once);
        _hubContextMock.Verify(x => x.Clients.Group($"auction-chat-{request.AuctionId}"), Times.Once);
        mockGroup.Verify(x => x.SendCoreAsync(
            "AuctionChatMessageReceived",
            It.Is<object[]>(payload => payload.Length == 1),
            default),
            Times.Once);
    }

    [Fact]
    public async Task CreateMessage_WithArgumentException_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateAuctionChatMessageRequest
        {
            AuctionId = 1,
            SenderId = 10,
            Content = string.Empty
        };

        _auctionChatServiceMock.Setup(x => x.AddMessageAsync(request))
            .ThrowsAsync(new ArgumentException("Content is required"));

        // Act
        var result = await _controller.CreateMessage(request);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task CreateMessage_WithException_ReturnsInternalServerError()
    {
        // Arrange
        var request = new CreateAuctionChatMessageRequest
        {
            AuctionId = 1,
            SenderId = 10,
            Content = "Message"
        };

        _auctionChatServiceMock.Setup(x => x.AddMessageAsync(request))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.CreateMessage(request);

        // Assert
        result.Result.Should().BeOfType<ObjectResult>();
        var objectResult = result.Result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
    }

    #endregion
}


