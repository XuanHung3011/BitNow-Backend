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

    /// <summary>
    /// Test ID: ADMIN-AUCTION-01
    /// Precondition: AuctionService hoạt động bình thường, có auctions trong hệ thống
    /// Input: searchTerm hợp lệ ("item"), statuses hợp lệ ("active,completed"), sortBy hợp lệ ("EndTime"), sortOrder hợp lệ ("desc"), page hợp lệ (1), pageSize hợp lệ (10)
    /// Condition: Lấy danh sách auctions với filter và sort hợp lệ
    /// Confirmation: HTTP 200 OK, trả về PaginatedResult với AuctionListItemDto
    /// Type: Normal
    /// Test Requirement: Kiểm tra chức năng lấy danh sách auctions với filter thành công
    /// </summary>
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

    /// <summary>
    /// Test ID: ADMIN-AUCTION-02
    /// Precondition: sortBy không hợp lệ
    /// Input: sortBy không hợp lệ ("InvalidField")
    /// Condition: Lấy auctions với sortBy không được phép
    /// Confirmation: HTTP 400 BadRequest, thông báo Invalid sortBy field
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra validation sortBy chỉ cho phép các field hợp lệ
    /// </summary>
    [Fact]
    public async Task GetAuctions_WithInvalidSortBy_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.GetAuctions(sortBy: "InvalidField");

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    /// <summary>
    /// Test ID: ADMIN-AUCTION-03
    /// Precondition: sortOrder không hợp lệ
    /// Input: sortOrder không hợp lệ ("invalid")
    /// Condition: Lấy auctions với sortOrder không được phép (chỉ cho phép "asc" hoặc "desc")
    /// Confirmation: HTTP 400 BadRequest, thông báo Invalid sortOrder
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra validation sortOrder chỉ cho phép "asc" hoặc "desc"
    /// </summary>
    [Fact]
    public async Task GetAuctions_WithInvalidSortOrder_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.GetAuctions(sortOrder: "invalid");

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    /// <summary>
    /// Test ID: ADMIN-AUCTION-04
    /// Precondition: statuses chứa giá trị không hợp lệ
    /// Input: statuses chứa status không hợp lệ ("active,invalid-status")
    /// Condition: Lấy auctions với status không được phép
    /// Confirmation: HTTP 400 BadRequest, thông báo Invalid status
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra validation statuses chỉ cho phép các status hợp lệ
    /// </summary>
    [Fact]
    public async Task GetAuctions_WithInvalidStatuses_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.GetAuctions(statuses: "active,invalid-status");

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    /// <summary>
    /// Test ID: ADMIN-AUCTION-05
    /// Precondition: Service gặp lỗi hệ thống
    /// Input: Tham số hợp lệ
    /// Condition: Service throw Exception (database error)
    /// Confirmation: HTTP 500 InternalServerError, thông báo Internal server error
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra xử lý exception từ service
    /// </summary>
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

    /// <summary>
    /// Test ID: ADMIN-AUCTION-06
    /// Precondition: Auction tồn tại trong hệ thống, AuctionService hoạt động bình thường
    /// Input: AuctionId hợp lệ (1)
    /// Condition: Lấy chi tiết auction theo ID hợp lệ
    /// Confirmation: HTTP 200 OK, trả về AuctionDetailDto với Id=1, ItemTitle="Item 1"
    /// Type: Normal
    /// Test Requirement: Kiểm tra chức năng lấy chi tiết auction thành công
    /// </summary>
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

    /// <summary>
    /// Test ID: ADMIN-AUCTION-07
    /// Precondition: Auction không tồn tại trong hệ thống
    /// Input: AuctionId không tồn tại (999)
    /// Condition: Lấy chi tiết auction với ID không tồn tại
    /// Confirmation: HTTP 404 NotFound, thông báo Auction not found
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra xử lý trường hợp auction không tồn tại
    /// </summary>
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

    /// <summary>
    /// Test ID: ADMIN-AUCTION-08
    /// Precondition: Service gặp lỗi hệ thống
    /// Input: AuctionId hợp lệ (1)
    /// Condition: Service throw Exception (database error)
    /// Confirmation: HTTP 500 InternalServerError, thông báo Internal server error
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra xử lý exception từ service
    /// </summary>
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

    /// <summary>
    /// Test ID: ADMIN-AUCTION-09
    /// Precondition: Status rỗng hoặc không hợp lệ
    /// Input: UpdateAuctionStatusRequest với Status rỗng ("")
    /// Condition: Cập nhật status với status rỗng
    /// Confirmation: HTTP 400 BadRequest, thông báo Status is required
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra validation status không được rỗng
    /// </summary>
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

    /// <summary>
    /// Test ID: ADMIN-AUCTION-10
    /// Precondition: Status không hợp lệ (không nằm trong danh sách cho phép)
    /// Input: UpdateAuctionStatusRequest với Status không hợp lệ ("invalid-status")
    /// Condition: Cập nhật status với giá trị không được phép
    /// Confirmation: HTTP 400 BadRequest, thông báo Invalid status
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra validation status chỉ cho phép các giá trị hợp lệ
    /// </summary>
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

    /// <summary>
    /// Test ID: ADMIN-AUCTION-11
    /// Precondition: Auction không tồn tại trong hệ thống
    /// Input: AuctionId không tồn tại (1), UpdateAuctionStatusRequest hợp lệ (Status="active")
    /// Condition: Cập nhật status cho auction không tồn tại
    /// Confirmation: HTTP 404 NotFound, thông báo Auction not found
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra xử lý trường hợp auction không tồn tại
    /// </summary>
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

    /// <summary>
    /// Test ID: ADMIN-AUCTION-12
    /// Precondition: Status = "paused" nhưng Reason quá ngắn (< 10 ký tự)
    /// Input: UpdateAuctionStatusRequest với Status="paused", Reason="Too short" (< 10 ký tự)
    /// Condition: Cập nhật status thành paused với reason không đủ dài
    /// Confirmation: HTTP 400 BadRequest, thông báo Reason must be at least 10 characters
    /// Type: Boundary/Abnormal
    /// Test Requirement: Kiểm tra validation reason phải >= 10 ký tự khi pause auction
    /// </summary>
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

    /// <summary>
    /// Test ID: ADMIN-AUCTION-13
    /// Precondition: Status = "paused" nhưng AdminSignature không hợp lệ
    /// Input: UpdateAuctionStatusRequest với Status="paused", Reason hợp lệ, AdminSignature sai ("WrongSignature")
    /// Condition: Cập nhật status thành paused với admin signature không hợp lệ
    /// Confirmation: HTTP 400 BadRequest, thông báo Invalid admin signature
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra validation admin signature khi pause auction
    /// </summary>
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

    /// <summary>
    /// Test ID: ADMIN-AUCTION-14
    /// Precondition: Auction tồn tại, Status hợp lệ và có thể transition, AuctionService hoạt động bình thường, SignalR HubContext hoạt động
    /// Input: AuctionId hợp lệ (1), UpdateAuctionStatusRequest hợp lệ (Status="completed")
    /// Condition: Cập nhật status auction và broadcast qua SignalR
    /// Confirmation: HTTP 204 NoContent, đồng thời broadcast status update qua SignalR
    /// Type: Normal
    /// Test Requirement: Kiểm tra chức năng cập nhật status auction thành công
    /// </summary>
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

    /// <summary>
    /// Test ID: ADMIN-AUCTION-15
    /// Precondition: Status transition không hợp lệ (ví dụ: từ "draft" sang "active" không được phép)
    /// Input: AuctionId hợp lệ (1), UpdateAuctionStatusRequest với Status="active", nhưng current status="draft"
    /// Condition: Service throw ArgumentException (Invalid status transition)
    /// Confirmation: HTTP 400 BadRequest, thông báo lỗi từ service
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra xử lý trường hợp status transition không hợp lệ
    /// </summary>
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

    /// <summary>
    /// Test ID: ADMIN-AUCTION-16
    /// Precondition: Auction không tồn tại trong hệ thống
    /// Input: AuctionId không tồn tại (1), ResumeAuctionRequest hợp lệ
    /// Condition: Resume auction không tồn tại
    /// Confirmation: HTTP 404 NotFound, thông báo Auction not found
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra xử lý trường hợp auction không tồn tại khi resume
    /// </summary>
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

    /// <summary>
    /// Test ID: ADMIN-AUCTION-17
    /// Precondition: Auction status không phải "paused"
    /// Input: AuctionId hợp lệ (1), Auction có Status="active" (không phải "paused")
    /// Condition: Resume auction với status không phải "paused"
    /// Confirmation: HTTP 400 BadRequest, thông báo Auction is not paused
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra validation chỉ cho phép resume auction có status="paused"
    /// </summary>
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

    /// <summary>
    /// Test ID: ADMIN-AUCTION-18
    /// Precondition: Auction đã hết hạn (EndTime đã qua)
    /// Input: AuctionId hợp lệ (1), Auction có Status="paused" nhưng EndTime đã qua
    /// Condition: Resume auction đã hết hạn
    /// Confirmation: HTTP 400 BadRequest, thông báo Auction has already ended
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra validation không cho phép resume auction đã hết hạn
    /// </summary>
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

    /// <summary>
    /// Test ID: ADMIN-AUCTION-19
    /// Precondition: Auction tồn tại, Status="paused", EndTime chưa qua, AuctionService hoạt động bình thường, SignalR HubContext hoạt động, có bidders và watchers
    /// Input: AuctionId hợp lệ (1), ResumeAuctionRequest hợp lệ (Reason="Resume after maintenance")
    /// Condition: Resume auction và gửi notification cho bidders/watchers, broadcast qua SignalR
    /// Confirmation: HTTP 204 NoContent, đồng thời tạo notifications và broadcast qua SignalR
    /// Type: Normal
    /// Test Requirement: Kiểm tra chức năng resume auction thành công và gửi notifications
    /// </summary>
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

    /// <summary>
    /// Test ID: ADMIN-AUCTION-20
    /// Precondition: Service gặp lỗi hệ thống
    /// Input: AuctionId hợp lệ (1), ResumeAuctionRequest hợp lệ
    /// Condition: Service throw Exception (database error)
    /// Confirmation: HTTP 500 InternalServerError, thông báo Internal server error
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra xử lý exception từ service
    /// </summary>
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


