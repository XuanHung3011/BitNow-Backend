using BitNow_Backend.BLL.IServices;
using BitNow_Backend.Controllers;
using BitNow_Backend.DAL.DTOs;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace BitNow_Backend.Tests.Controllers;

public class AutoBidsControllerTests
{
    private readonly Mock<IAutoBidService> _autoBidServiceMock;
    private readonly Mock<ILogger<AutoBidsController>> _loggerMock;
    private readonly AutoBidsController _controller;

    public AutoBidsControllerTests()
    {
        _autoBidServiceMock = new Mock<IAutoBidService>();
        _loggerMock = new Mock<ILogger<AutoBidsController>>();
        _controller = new AutoBidsController(_autoBidServiceMock.Object, _loggerMock.Object);
    }

    #region CreateOrUpdate

    /// <summary>
    /// Test ID: AUTO-01
    /// Precondition: Auction tồn tại và đang active, User hợp lệ, AutoBidService hoạt động bình thường
    /// Input: AuctionId hợp lệ (1), UserId hợp lệ (2), MaxAmount hợp lệ (1000)
    /// Condition: Tạo hoặc cập nhật auto bid với thông tin hợp lệ
    /// Confirmation: HTTP 200 OK, trả về AutoBidDto với Id, AuctionId, UserId, MaxAmount đúng
    /// Type: Normal
    /// Test Requirement: Kiểm tra chức năng tạo/cập nhật auto bid thành công
    /// </summary>
    [Fact]
    public async Task CreateOrUpdate_WithValidRequest_ReturnsOk()
    {
        // Arrange
        var dto = new CreateAutoBidDto
        {
            AuctionId = 1,
            UserId = 2,
            MaxAmount = 1000
        };
        var expected = new AutoBidDto
        {
            Id = 10,
            AuctionId = dto.AuctionId,
            UserId = dto.UserId,
            MaxAmount = dto.MaxAmount,
            IsActive = true
        };

        _autoBidServiceMock.Setup(x => x.CreateOrUpdateAutoBidAsync(dto.AuctionId, dto.UserId, dto.MaxAmount))
            .ReturnsAsync(expected);

        // Act
        var result = await _controller.CreateOrUpdate(dto);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var ok = result.Result as OkObjectResult;
        ok!.Value.Should().BeEquivalentTo(expected);
    }

    /// <summary>
    /// Test ID: AUTO-02
    /// Precondition: DTO không hợp lệ
    /// Input: MaxAmount = 0 (giá trị biên không hợp lệ)
    /// Condition: Tạo auto bid với MaxAmount <= 0
    /// Confirmation: HTTP 400 BadRequest, thông báo Invalid request
    /// Type: Boundary/Abnormal
    /// Test Requirement: Kiểm tra validation MaxAmount phải > 0
    /// </summary>
    [Fact]
    public async Task CreateOrUpdate_WithInvalidRequest_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.CreateOrUpdate(new CreateAutoBidDto
        {
            AuctionId = 1,
            UserId = 2,
            MaxAmount = 0
        });

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    /// <summary>
    /// Test ID: AUTO-03
    /// Precondition: Auction không tồn tại hoặc không đủ điều kiện, Service throw InvalidOperationException
    /// Input: AuctionId hợp lệ (1), UserId hợp lệ (2), MaxAmount hợp lệ (500)
    /// Condition: Tạo auto bid nhưng vi phạm business rule (ví dụ: auction đã đóng)
    /// Confirmation: HTTP 400 BadRequest, thông báo lỗi từ service
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra xử lý trường hợp vi phạm business rule
    /// </summary>
    [Fact]
    public async Task CreateOrUpdate_WhenServiceThrowsInvalidOperation_ReturnsBadRequest()
    {
        // Arrange
        var dto = new CreateAutoBidDto
        {
            AuctionId = 1,
            UserId = 2,
            MaxAmount = 500
        };

        _autoBidServiceMock.Setup(x => x.CreateOrUpdateAutoBidAsync(dto.AuctionId, dto.UserId, dto.MaxAmount))
            .ThrowsAsync(new InvalidOperationException("rule violated"));

        // Act
        var result = await _controller.CreateOrUpdate(dto);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    /// <summary>
    /// Test ID: AUTO-04
    /// Precondition: Service gặp lỗi hệ thống
    /// Input: AuctionId hợp lệ (1), UserId hợp lệ (2), MaxAmount hợp lệ (500)
    /// Condition: Service throw Exception (database error)
    /// Confirmation: HTTP 500 InternalServerError, thông báo Internal server error
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra xử lý exception từ service
    /// </summary>
    [Fact]
    public async Task CreateOrUpdate_WhenServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var dto = new CreateAutoBidDto
        {
            AuctionId = 1,
            UserId = 2,
            MaxAmount = 500
        };

        _autoBidServiceMock.Setup(x => x.CreateOrUpdateAutoBidAsync(dto.AuctionId, dto.UserId, dto.MaxAmount))
            .ThrowsAsync(new Exception("db error"));

        // Act
        var result = await _controller.CreateOrUpdate(dto);

        // Assert
        result.Result.Should().BeOfType<ObjectResult>();
        var objectResult = result.Result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
    }

    #endregion

    #region Get

    /// <summary>
    /// Test ID: AUTO-05
    /// Precondition: Auto bid tồn tại trong hệ thống cho auction và user cụ thể
    /// Input: AuctionId hợp lệ (3), UserId hợp lệ (4)
    /// Condition: Lấy thông tin auto bid đã tồn tại
    /// Confirmation: HTTP 200 OK, trả về AutoBidDto với thông tin đầy đủ
    /// Type: Normal
    /// Test Requirement: Kiểm tra chức năng lấy thông tin auto bid thành công
    /// </summary>
    [Fact]
    public async Task Get_WithExistingAutoBid_ReturnsOk()
    {
        // Arrange
        var expected = new AutoBidDto
        {
            Id = 11,
            AuctionId = 3,
            UserId = 4,
            MaxAmount = 1500
        };

        _autoBidServiceMock.Setup(x => x.GetAutoBidAsync(expected.AuctionId, expected.UserId))
            .ReturnsAsync(expected);

        // Act
        var result = await _controller.Get(expected.AuctionId, expected.UserId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var ok = result.Result as OkObjectResult;
        ok!.Value.Should().BeEquivalentTo(expected);
    }

    /// <summary>
    /// Test ID: AUTO-06
    /// Precondition: Auto bid không tồn tại trong hệ thống
    /// Input: AuctionId hợp lệ (1), UserId hợp lệ (2) nhưng không có auto bid
    /// Condition: Lấy thông tin auto bid không tồn tại
    /// Confirmation: HTTP 404 NotFound, thông báo Auto bid not found
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra xử lý trường hợp auto bid không tồn tại
    /// </summary>
    [Fact]
    public async Task Get_WithNonExistingAutoBid_ReturnsNotFound()
    {
        // Arrange
        _autoBidServiceMock.Setup(x => x.GetAutoBidAsync(1, 2))
            .ReturnsAsync((AutoBidDto?)null);

        // Act
        var result = await _controller.Get(1, 2);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    /// <summary>
    /// Test ID: AUTO-07
    /// Precondition: Service gặp lỗi hệ thống
    /// Input: AuctionId hợp lệ (1), UserId hợp lệ (2)
    /// Condition: Service throw Exception (database error)
    /// Confirmation: HTTP 500 InternalServerError, thông báo Internal server error
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra xử lý exception từ service
    /// </summary>
    [Fact]
    public async Task Get_WhenServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        _autoBidServiceMock.Setup(x => x.GetAutoBidAsync(1, 2))
            .ThrowsAsync(new Exception("db error"));

        // Act
        var result = await _controller.Get(1, 2);

        // Assert
        result.Result.Should().BeOfType<ObjectResult>();
        var objectResult = result.Result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
    }

    #endregion

    #region Deactivate

    /// <summary>
    /// Test ID: AUTO-08
    /// Precondition: Auto bid tồn tại và đang active
    /// Input: AuctionId hợp lệ (1), UserId hợp lệ (2)
    /// Condition: Hủy kích hoạt auto bid đang tồn tại
    /// Confirmation: HTTP 200 OK, thông báo Auto bid deactivated successfully
    /// Type: Normal
    /// Test Requirement: Kiểm tra chức năng hủy auto bid thành công
    /// </summary>
    [Fact]
    public async Task Deactivate_WithExistingAutoBid_ReturnsOk()
    {
        // Arrange
        _autoBidServiceMock.Setup(x => x.DeactivateAutoBidAsync(1, 2))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.Deactivate(1, 2);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var ok = result as OkObjectResult;
        ok!.Value.Should().BeEquivalentTo(new { message = "Auto bid deactivated successfully" });
    }

    /// <summary>
    /// Test ID: AUTO-09
    /// Precondition: Auto bid không tồn tại trong hệ thống
    /// Input: AuctionId hợp lệ (1), UserId hợp lệ (2) nhưng không có auto bid
    /// Condition: Hủy auto bid không tồn tại
    /// Confirmation: HTTP 404 NotFound, thông báo Auto bid not found
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra xử lý trường hợp auto bid không tồn tại khi hủy
    /// </summary>
    [Fact]
    public async Task Deactivate_WithNonExistingAutoBid_ReturnsNotFound()
    {
        // Arrange
        _autoBidServiceMock.Setup(x => x.DeactivateAutoBidAsync(1, 2))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.Deactivate(1, 2);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    /// <summary>
    /// Test ID: AUTO-10
    /// Precondition: Service gặp lỗi hệ thống
    /// Input: AuctionId hợp lệ (1), UserId hợp lệ (2)
    /// Condition: Service throw Exception (database error)
    /// Confirmation: HTTP 500 InternalServerError, thông báo Internal server error
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra xử lý exception từ service
    /// </summary>
    [Fact]
    public async Task Deactivate_WhenServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        _autoBidServiceMock.Setup(x => x.DeactivateAutoBidAsync(1, 2))
            .ThrowsAsync(new Exception("db error"));

        // Act
        var result = await _controller.Deactivate(1, 2);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
    }

    #endregion

    #region GetBidIncrement

    /// <summary>
    /// Test ID: AUTO-11
    /// Precondition: Service tính toán bid increment hoạt động bình thường
    /// Input: CurrentPrice hợp lệ (100) - giá trị normal
    /// Condition: Tính toán bước nhảy giá cho giá hiện tại hợp lệ
    /// Confirmation: HTTP 200 OK, trả về increment = 5
    /// Type: Normal
    /// Test Requirement: Kiểm tra chức năng tính toán bid increment thành công
    /// </summary>
    [Fact]
    public void GetBidIncrement_WithValidPrice_ReturnsOk()
    {
        // Arrange
        _autoBidServiceMock.Setup(x => x.CalculateBidIncrement(100m))
            .Returns(5m);

        // Act
        var result = _controller.GetBidIncrement(100m);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var ok = result.Result as OkObjectResult;
        ok!.Value.Should().BeEquivalentTo(new { increment = 5m });
    }

    /// <summary>
    /// Test ID: AUTO-12
    /// Precondition: Service gặp lỗi khi tính toán
    /// Input: CurrentPrice hợp lệ (50)
    /// Condition: Service throw Exception khi tính toán increment
    /// Confirmation: HTTP 500 InternalServerError, thông báo Internal server error
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra xử lý exception từ service khi tính toán
    /// </summary>
    [Fact]
    public void GetBidIncrement_WhenServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        _autoBidServiceMock.Setup(x => x.CalculateBidIncrement(It.IsAny<decimal>()))
            .Throws(new Exception("calc error"));

        // Act
        var result = _controller.GetBidIncrement(50m);

        // Assert
        result.Result.Should().BeOfType<ObjectResult>();
        var objectResult = result.Result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
    }

    #endregion
}

