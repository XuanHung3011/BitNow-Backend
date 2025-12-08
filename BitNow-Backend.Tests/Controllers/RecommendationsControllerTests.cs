using BitNow_Backend.BLL.IServices;
using BitNow_Backend.Controllers;
using BitNow_Backend.DAL.DTOs;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace BitNow_Backend.Tests.Controllers;

public class RecommendationsControllerTests
{
    private readonly Mock<IRecommendationService> _recommendationServiceMock;
    private readonly Mock<IVectorSyncService> _vectorSyncServiceMock;
    private readonly Mock<ILogger<RecommendationsController>> _loggerMock;
    private readonly RecommendationsController _controller;

    public RecommendationsControllerTests()
    {
        _recommendationServiceMock = new Mock<IRecommendationService>();
        _vectorSyncServiceMock = new Mock<IVectorSyncService>();
        _loggerMock = new Mock<ILogger<RecommendationsController>>();
        _controller = new RecommendationsController(_recommendationServiceMock.Object, _vectorSyncServiceMock.Object, _loggerMock.Object);
    }

    #region GetPersonalized

    /// <summary>
    /// Test ID: RECO-01
    /// Precondition: User tồn tại trong hệ thống, RecommendationService hoạt động bình thường
    /// Input: UserId hợp lệ (1), limit hợp lệ (8)
    /// Condition: Lấy danh sách items được gợi ý cá nhân hóa cho user
    /// Confirmation: HTTP 200 OK, trả về danh sách ItemResponseDto được gợi ý
    /// Type: Normal
    /// Test Requirement: Kiểm tra chức năng lấy personalized recommendations thành công
    /// </summary>
    [Fact]
    public async Task GetPersonalized_WithValidUserId_ReturnsOk()
    {
        // Arrange
        var expectedItems = new List<ItemResponseDto>
        {
            new()
            {
                Id = 1,
                Title = "Recommended Item",
                Status = "approved"
            }
        };

        _recommendationServiceMock.Setup(x => x.GetPersonalizedItemsAsync(1, 8, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedItems);

        // Act
        var result = await _controller.GetPersonalized(1, 8);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(expectedItems);
    }

    /// <summary>
    /// Test ID: RECO-02
    /// Precondition: UserId không hợp lệ
    /// Input: UserId = 0 (giá trị biên không hợp lệ)
    /// Condition: Lấy recommendations với UserId = 0
    /// Confirmation: HTTP 400 BadRequest, thông báo userId is required and must be greater than 0
    /// Type: Boundary/Abnormal
    /// Test Requirement: Kiểm tra validation UserId phải > 0
    /// </summary>
    [Fact]
    public async Task GetPersonalized_WithUserIdZero_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.GetPersonalized(0, 8);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    /// <summary>
    /// Test ID: RECO-03
    /// Precondition: UserId không hợp lệ
    /// Input: UserId = -1 (giá trị không hợp lệ)
    /// Condition: Lấy recommendations với UserId âm
    /// Confirmation: HTTP 400 BadRequest, thông báo userId is required and must be greater than 0
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra validation UserId không được âm
    /// </summary>
    [Fact]
    public async Task GetPersonalized_WithNegativeUserId_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.GetPersonalized(-1, 8);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    /// <summary>
    /// Test ID: RECO-04
    /// Precondition: Service gặp lỗi hệ thống
    /// Input: UserId hợp lệ (1), limit hợp lệ (8)
    /// Condition: Service throw Exception (Service error)
    /// Confirmation: HTTP 500 InternalServerError, thông báo Internal server error
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra xử lý exception từ service
    /// </summary>
    [Fact]
    public async Task GetPersonalized_WithException_ReturnsInternalServerError()
    {
        // Arrange
        _recommendationServiceMock.Setup(x => x.GetPersonalizedItemsAsync(1, 8, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Service error"));

        // Act
        var result = await _controller.GetPersonalized(1, 8);

        // Assert
        result.Result.Should().BeOfType<ObjectResult>();
        var objectResult = result.Result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
    }

    #endregion
}

