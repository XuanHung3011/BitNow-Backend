using BitNow_Backend.BLL.IServices;
using BitNow_Backend.Controllers;
using BitNow_Backend.DAL.DTOs;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace BitNow_Backend.Tests.Controllers;

public class RatingsControllerTests
{
    private readonly Mock<IRatingService> _ratingServiceMock;
    private readonly Mock<ILogger<RatingsController>> _loggerMock;
    private readonly RatingsController _controller;

    public RatingsControllerTests()
    {
        _ratingServiceMock = new Mock<IRatingService>();
        _loggerMock = new Mock<ILogger<RatingsController>>();
        _controller = new RatingsController(_ratingServiceMock.Object, _loggerMock.Object);
    }

    #region Create

    /// <summary>
    /// Test ID: RATE-01
    /// Precondition: Auction tồn tại và đã completed, User hợp lệ, RatingService hoạt động bình thường
    /// Input: RatingCreateDto hợp lệ (AuctionId=1, RaterId=1, RatedId=2, Rating=5, Comment="Great seller")
    /// Condition: Tạo rating và feedback sau khi auction hoàn thành
    /// Confirmation: HTTP 200 OK, trả về RatingResponseDto với Id=1, Rating=5
    /// Type: Normal
    /// Test Requirement: Kiểm tra chức năng tạo rating thành công
    /// </summary>
    [Fact]
    public async Task Create_WithValidDto_ReturnsOk()
    {
        // Arrange
        var dto = new RatingCreateDto
        {
            AuctionId = 1,
            RaterId = 1,
            RatedId = 2,
            Rating = 5,
            Comment = "Great seller"
        };

        var expectedRating = new RatingResponseDto
        {
            Id = 1,
            AuctionId = 1,
            RaterId = 1,
            RatedId = 2,
            Rating = 5,
            Comment = "Great seller",
            CreatedAt = DateTime.Now
        };

        _ratingServiceMock.Setup(x => x.CreateAsync(dto))
            .ReturnsAsync(expectedRating);

        // Act
        var result = await _controller.Create(dto);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(expectedRating);
    }

    /// <summary>
    /// Test ID: RATE-02
    /// Precondition: ModelState không hợp lệ
    /// Input: RatingCreateDto với AuctionId=0 (giá trị không hợp lệ)
    /// Condition: Tạo rating với dữ liệu không hợp lệ theo validation rules
    /// Confirmation: HTTP 400 BadRequest, không gọi CreateAsync
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra validation model state trước khi xử lý
    /// </summary>
    [Fact]
    public async Task Create_WithInvalidModelState_ReturnsBadRequest()
    {
        // Arrange
        var dto = new RatingCreateDto
        {
            AuctionId = 0, // Invalid
            RaterId = 1,
            RatedId = 2,
            Rating = 5
        };

        _controller.ModelState.AddModelError("AuctionId", "AuctionId is required");

        // Act
        var result = await _controller.Create(dto);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    /// <summary>
    /// Test ID: RATE-03
    /// Precondition: Auction không tồn tại hoặc không đủ điều kiện để rating
    /// Input: RatingCreateDto hợp lệ nhưng auction không tồn tại
    /// Condition: Service throw InvalidOperationException (Auction not found)
    /// Confirmation: HTTP 400 BadRequest, thông báo lỗi từ service
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra xử lý trường hợp auction không tồn tại
    /// </summary>
    [Fact]
    public async Task Create_WithInvalidOperationException_ReturnsBadRequest()
    {
        // Arrange
        var dto = new RatingCreateDto
        {
            AuctionId = 1,
            RaterId = 1,
            RatedId = 2,
            Rating = 5
        };

        _ratingServiceMock.Setup(x => x.CreateAsync(dto))
            .ThrowsAsync(new InvalidOperationException("Auction not found"));

        // Act
        var result = await _controller.Create(dto);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    /// <summary>
    /// Test ID: RATE-04
    /// Precondition: Service gặp lỗi hệ thống
    /// Input: RatingCreateDto hợp lệ
    /// Condition: Service throw Exception (database error)
    /// Confirmation: HTTP 500 InternalServerError, thông báo Internal server error
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra xử lý exception từ service
    /// </summary>
    [Fact]
    public async Task Create_WithException_ReturnsInternalServerError()
    {
        // Arrange
        var dto = new RatingCreateDto
        {
            AuctionId = 1,
            RaterId = 1,
            RatedId = 2,
            Rating = 5
        };

        _ratingServiceMock.Setup(x => x.CreateAsync(dto))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.Create(dto);

        // Assert
        result.Result.Should().BeOfType<ObjectResult>();
        var objectResult = result.Result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
    }

    #endregion

    #region GetForUser

    /// <summary>
    /// Test ID: RATE-05
    /// Precondition: User tồn tại trong hệ thống, có ratings, RatingService hoạt động bình thường
    /// Input: UserId hợp lệ (1), page hợp lệ (1), pageSize hợp lệ (10)
    /// Condition: Lấy danh sách ratings của user với tham số hợp lệ
    /// Confirmation: HTTP 200 OK, trả về danh sách RatingResponseDto
    /// Type: Normal
    /// Test Requirement: Kiểm tra chức năng lấy ratings của user thành công
    /// </summary>
    [Fact]
    public async Task GetForUser_WithValidParams_ReturnsOk()
    {
        // Arrange
        var expectedRatings = new List<RatingResponseDto>
        {
            new()
            {
                Id = 1,
                AuctionId = 1,
                RaterId = 1,
                RatedId = 2,
                Rating = 5,
                CreatedAt = DateTime.Now
            }
        };

        _ratingServiceMock.Setup(x => x.GetForUserAsync(1, 1, 10))
            .ReturnsAsync(expectedRatings);

        // Act
        var result = await _controller.GetForUser(1, 1, 10);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(expectedRatings);
    }

    /// <summary>
    /// Test ID: RATE-06
    /// Precondition: Service gặp lỗi hệ thống
    /// Input: UserId hợp lệ (1), page hợp lệ (1), pageSize hợp lệ (10)
    /// Condition: Service throw Exception (database error)
    /// Confirmation: HTTP 500 InternalServerError, thông báo Internal server error
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra xử lý exception từ service
    /// </summary>
    [Fact]
    public async Task GetForUser_WithException_ReturnsInternalServerError()
    {
        // Arrange
        _ratingServiceMock.Setup(x => x.GetForUserAsync(1, 1, 10))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetForUser(1, 1, 10);

        // Assert
        result.Result.Should().BeOfType<ObjectResult>();
        var objectResult = result.Result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
    }

    #endregion

    #region GetForAuction

    /// <summary>
    /// Test ID: RATE-07
    /// Precondition: Auction tồn tại trong hệ thống, có ratings, RatingService hoạt động bình thường
    /// Input: AuctionId hợp lệ (1)
    /// Condition: Lấy danh sách ratings của auction
    /// Confirmation: HTTP 200 OK, trả về danh sách RatingResponseDto
    /// Type: Normal
    /// Test Requirement: Kiểm tra chức năng lấy ratings của auction thành công
    /// </summary>
    [Fact]
    public async Task GetForAuction_WithValidAuctionId_ReturnsOk()
    {
        // Arrange
        var expectedRatings = new List<RatingResponseDto>
        {
            new()
            {
                Id = 1,
                AuctionId = 1,
                RaterId = 1,
                RatedId = 2,
                Rating = 5,
                CreatedAt = DateTime.Now
            }
        };

        _ratingServiceMock.Setup(x => x.GetForAuctionAsync(1))
            .ReturnsAsync(expectedRatings);

        // Act
        var result = await _controller.GetForAuction(1);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(expectedRatings);
    }

    /// <summary>
    /// Test ID: RATE-08
    /// Precondition: Service gặp lỗi hệ thống
    /// Input: AuctionId hợp lệ (1)
    /// Condition: Service throw Exception (database error)
    /// Confirmation: HTTP 500 InternalServerError, thông báo Internal server error
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra xử lý exception từ service
    /// </summary>
    [Fact]
    public async Task GetForAuction_WithException_ReturnsInternalServerError()
    {
        // Arrange
        _ratingServiceMock.Setup(x => x.GetForAuctionAsync(1))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetForAuction(1);

        // Assert
        result.Result.Should().BeOfType<ObjectResult>();
        var objectResult = result.Result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
    }

    #endregion
}

