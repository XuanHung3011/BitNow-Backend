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

public class AdminAuctionsControllerTests
{
    private readonly Mock<IAuctionService> _auctionServiceMock;
    private readonly Mock<INotificationService> _notificationServiceMock;
    private readonly Mock<IBidService> _bidServiceMock;
    private readonly Mock<IWatchlistService> _watchlistServiceMock;
    private readonly Mock<IHubContext<AuctionHub>> _auctionHubMock;
    private readonly Mock<ILogger<AdminAuctionsController>> _loggerMock;
    private readonly AdminAuctionsController _controller;

    public AdminAuctionsControllerTests()
    {
        _auctionServiceMock = new Mock<IAuctionService>();
        _notificationServiceMock = new Mock<INotificationService>();
        _bidServiceMock = new Mock<IBidService>();
        _watchlistServiceMock = new Mock<IWatchlistService>();
        _auctionHubMock = new Mock<IHubContext<AuctionHub>>();
        _loggerMock = new Mock<ILogger<AdminAuctionsController>>();

        _controller = new AdminAuctionsController(
            _auctionServiceMock.Object,
            _loggerMock.Object,
            _auctionHubMock.Object,
            _notificationServiceMock.Object,
            _bidServiceMock.Object,
            _watchlistServiceMock.Object);
    }

    #region GetAuctions

    [Fact]
    public async Task GetAuctions_WithValidParams_ReturnsOk()
    {
        // Arrange
        var expectedResult = new PaginatedResult<AuctionListItemDto>
        {
            Data = new List<AuctionListItemDto>
            {
                new()
                {
                    Id = 1,
                    ItemTitle = "Item 1",
                    Status = "active"
                }
            },
            TotalCount = 1,
            Page = 1,
            PageSize = 10
        };

        _auctionServiceMock.Setup(x => x.GetAuctionsWithFilterAsync(It.IsAny<AuctionFilterDto>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetAuctions(
            searchTerm: "item",
            statuses: "active,completed",
            sortBy: "EndTime",
            sortOrder: "desc",
            page: 1,
            pageSize: 10);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(expectedResult);
    }

    [Fact]
    public async Task GetAuctions_WithInvalidSortBy_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.GetAuctions(sortBy: "InvalidField");

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetAuctions_WithInvalidSortOrder_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.GetAuctions(sortOrder: "invalid");

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetAuctions_WithInvalidStatuses_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.GetAuctions(statuses: "active,invalid-status");

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetAuctions_WithException_ReturnsInternalServerError()
    {
        // Arrange
        _auctionServiceMock.Setup(x => x.GetAuctionsWithFilterAsync(It.IsAny<AuctionFilterDto>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetAuctions();

        // Assert
        result.Result.Should().BeOfType<ObjectResult>();
        var objectResult = result.Result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
    }

    #endregion

    #region GetAuctionDetail

    [Fact]
    public async Task GetAuctionDetail_WithExistingId_ReturnsOk()
    {
        // Arrange
        var auction = new AuctionDetailDto
        {
            Id = 1,
            ItemTitle = "Item 1",
            Status = "active"
        };

        _auctionServiceMock.Setup(x => x.GetDetailAsync(1))
            .ReturnsAsync(auction);

        // Act
        var result = await _controller.GetAuctionDetail(1);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(auction);
    }

    [Fact]
    public async Task GetAuctionDetail_WithNonExistingId_ReturnsNotFound()
    {
        // Arrange
        _auctionServiceMock.Setup(x => x.GetDetailAsync(999))
            .ReturnsAsync((AuctionDetailDto?)null);

        // Act
        var result = await _controller.GetAuctionDetail(999);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetAuctionDetail_WithException_ReturnsInternalServerError()
    {
        // Arrange
        _auctionServiceMock.Setup(x => x.GetDetailAsync(1))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetAuctionDetail(1);

        // Assert
        result.Result.Should().BeOfType<ObjectResult>();
        var objectResult = result.Result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
    }

    #endregion

    #region UpdateStatus

    [Fact]
    public async Task UpdateStatus_WithMissingStatus_ReturnsBadRequest()
    {
        // Arrange
        var request = new AdminAuctionsController.UpdateAuctionStatusRequest
        {
            Status = ""
        };

        // Act
        var result = await _controller.UpdateStatus(1, request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task UpdateStatus_WithInvalidStatus_ReturnsBadRequest()
    {
        // Arrange
        var request = new AdminAuctionsController.UpdateAuctionStatusRequest
        {
            Status = "invalid-status"
        };

        // Act
        var result = await _controller.UpdateStatus(1, request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task UpdateStatus_WhenAuctionNotFound_ReturnsNotFound()
    {
        // Arrange
        var request = new AdminAuctionsController.UpdateAuctionStatusRequest
        {
            Status = "active"
        };

        _auctionServiceMock.Setup(x => x.GetDetailAsync(1))
            .ReturnsAsync((AuctionDetailDto?)null);

        // Act
        var result = await _controller.UpdateStatus(1, request);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task UpdateStatus_PausedWithInvalidReason_ReturnsBadRequest()
    {
        // Arrange
        var request = new AdminAuctionsController.UpdateAuctionStatusRequest
        {
            Status = "paused",
            Reason = "Too short",
            AdminSignature = "Admin"
        };

        var auction = new AuctionDetailDto
        {
            Id = 1,
            ItemTitle = "Item 1",
            Status = "active",
            SellerId = 10
        };

        _auctionServiceMock.Setup(x => x.GetDetailAsync(1))
            .ReturnsAsync(auction);

        // Act
        var result = await _controller.UpdateStatus(1, request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task UpdateStatus_PausedWithInvalidSignature_ReturnsBadRequest()
    {
        // Arrange
        var request = new AdminAuctionsController.UpdateAuctionStatusRequest
        {
            Status = "paused",
            Reason = "Nguyên nhân hợp lệ đủ dài",
            AdminSignature = "WrongSignature"
        };

        var auction = new AuctionDetailDto
        {
            Id = 1,
            ItemTitle = "Item 1",
            Status = "active",
            SellerId = 10
        };

        _auctionServiceMock.Setup(x => x.GetDetailAsync(1))
            .ReturnsAsync(auction);

        // Act
        var result = await _controller.UpdateStatus(1, request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task UpdateStatus_WithValidData_ReturnsNoContent()
    {
        // Arrange
        var request = new AdminAuctionsController.UpdateAuctionStatusRequest
        {
            Status = "completed"
        };

        var auction = new AuctionDetailDto
        {
            Id = 1,
            ItemTitle = "Item 1",
            Status = "active",
            SellerId = 10
        };

        _auctionServiceMock.Setup(x => x.GetDetailAsync(1))
            .ReturnsAsync(auction);

        _auctionServiceMock.Setup(x => x.UpdateStatusAsync(1, "completed"))
            .ReturnsAsync(true);

        // Mock HubContext clients
        var mockClients = new Mock<IHubClients>();
        var mockGroup = new Mock<IClientProxy>();
        _auctionHubMock.Setup(x => x.Clients).Returns(mockClients.Object);
        _auctionHubMock.Setup(x => x.Clients.Group(It.IsAny<string>())).Returns(mockGroup.Object);
        mockGroup.Setup(x => x.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), default))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.UpdateStatus(1, request);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        _auctionServiceMock.Verify(x => x.UpdateStatusAsync(1, "completed"), Times.Once);
    }

    [Fact]
    public async Task UpdateStatus_WithArgumentException_ReturnsBadRequest()
    {
        // Arrange
        var request = new AdminAuctionsController.UpdateAuctionStatusRequest
        {
            Status = "active"
        };

        var auction = new AuctionDetailDto
        {
            Id = 1,
            ItemTitle = "Item 1",
            Status = "draft",
            SellerId = 10
        };

        _auctionServiceMock.Setup(x => x.GetDetailAsync(1))
            .ReturnsAsync(auction);

        _auctionServiceMock.Setup(x => x.UpdateStatusAsync(1, "active"))
            .ThrowsAsync(new ArgumentException("Invalid status transition"));

        // Act
        var result = await _controller.UpdateStatus(1, request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region ResumeAuction

    [Fact]
    public async Task ResumeAuction_WhenAuctionNotFound_ReturnsNotFound()
    {
        // Arrange
        _auctionServiceMock.Setup(x => x.GetDetailAsync(1))
            .ReturnsAsync((AuctionDetailDto?)null);

        // Act
        var result = await _controller.ResumeAuction(1, new AdminAuctionsController.ResumeAuctionRequest());

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task ResumeAuction_WhenStatusIsNotPaused_ReturnsBadRequest()
    {
        // Arrange
        var auction = new AuctionDetailDto
        {
            Id = 1,
            Status = "active",
            EndTime = DateTime.Now.AddHours(1),
            SellerId = 10
        };

        _auctionServiceMock.Setup(x => x.GetDetailAsync(1))
            .ReturnsAsync(auction);

        // Act
        var result = await _controller.ResumeAuction(1, new AdminAuctionsController.ResumeAuctionRequest());

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task ResumeAuction_WhenEndTimePassed_ReturnsBadRequest()
    {
        // Arrange
        var auction = new AuctionDetailDto
        {
            Id = 1,
            Status = "paused",
            EndTime = DateTime.Now.AddMinutes(-5),
            SellerId = 10
        };

        _auctionServiceMock.Setup(x => x.GetDetailAsync(1))
            .ReturnsAsync(auction);

        // Act
        var result = await _controller.ResumeAuction(1, new AdminAuctionsController.ResumeAuctionRequest());

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task ResumeAuction_WithValidData_ReturnsNoContent()
    {
        // Arrange
        var auction = new AuctionDetailDto
        {
            Id = 1,
            ItemTitle = "Item 1",
            Status = "paused",
            EndTime = DateTime.Now.AddHours(1),
            SellerId = 10
        };

        _auctionServiceMock.Setup(x => x.GetDetailAsync(1))
            .ReturnsAsync(auction);

        _auctionServiceMock.Setup(x => x.ResumeAuctionAsync(1))
            .ReturnsAsync(true);

        // Mock HubContext clients
        var mockClients = new Mock<IHubClients>();
        var mockGroup = new Mock<IClientProxy>();
        _auctionHubMock.Setup(x => x.Clients).Returns(mockClients.Object);
        _auctionHubMock.Setup(x => x.Clients.Group(It.IsAny<string>())).Returns(mockGroup.Object);
        mockGroup.Setup(x => x.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), default))
            .Returns(Task.CompletedTask);

        // Mock notification dependencies used in NotifyAuctionParticipantsAsync
        _bidServiceMock.Setup(x => x.GetDistinctBidderIdsByAuctionAsync(1))
            .ReturnsAsync(new List<int> { 20, 30 });
        _watchlistServiceMock.Setup(x => x.GetDistinctUserIdsByAuctionAsync(1))
            .ReturnsAsync(new List<int> { 40 });
        _notificationServiceMock.Setup(x => x.CreateNotificationAsync(It.IsAny<CreateNotificationDto>()))
            .ReturnsAsync(new NotificationResponseDto
            {
                Id = 1,
                UserId = 10,
                Message = "test notification",
                IsRead = false,
                CreatedAt = DateTime.Now
            });

        var request = new AdminAuctionsController.ResumeAuctionRequest
        {
            Reason = "Resume after maintenance"
        };

        // Act
        var result = await _controller.ResumeAuction(1, request);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        _auctionServiceMock.Verify(x => x.ResumeAuctionAsync(1), Times.Once);
        _notificationServiceMock.Verify(
            x => x.CreateNotificationAsync(It.IsAny<CreateNotificationDto>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task ResumeAuction_WithException_ReturnsInternalServerError()
    {
        // Arrange
        var auction = new AuctionDetailDto
        {
            Id = 1,
            Status = "paused",
            EndTime = DateTime.Now.AddHours(1),
            SellerId = 10
        };

        _auctionServiceMock.Setup(x => x.GetDetailAsync(1))
            .ReturnsAsync(auction);

        _auctionServiceMock.Setup(x => x.ResumeAuctionAsync(1))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.ResumeAuction(1, new AdminAuctionsController.ResumeAuctionRequest());

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
    }

    #endregion
}


