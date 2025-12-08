using BitNow_Backend.BLL.IServices;
using BitNow_Backend.Controllers;
using BitNow_Backend.DAL.DTOs;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace BitNow_Backend.Tests.Controllers;

public class PlatformAnalyticsControllerTests
{
    private readonly Mock<IPlatformAnalyticsService> _platformAnalyticsServiceMock;
    private readonly Mock<ILogger<PlatformAnalyticsController>> _loggerMock;
    private readonly PlatformAnalyticsController _controller;

    public PlatformAnalyticsControllerTests()
    {
        _platformAnalyticsServiceMock = new Mock<IPlatformAnalyticsService>();
        _loggerMock = new Mock<ILogger<PlatformAnalyticsController>>();
        _controller = new PlatformAnalyticsController(_platformAnalyticsServiceMock.Object, _loggerMock.Object);
    }

    #region GetPlatformAnalytics

    /// <summary>
    /// Test ID: ANALYTICS-01
    /// Precondition: PlatformAnalyticsService hoạt động bình thường, có dữ liệu analytics
    /// Input: Không có tham số đầu vào
    /// Condition: Lấy tổng quan analytics của platform
    /// Confirmation: HTTP 200 OK, trả về object với newUsers, newAuctions, totalTransactions, successRate
    /// Type: Normal
    /// Test Requirement: Kiểm tra chức năng lấy platform analytics thành công
    /// </summary>
    [Fact]
    public async Task GetPlatformAnalytics_WithValidData_ReturnsOk()
    {
        // Arrange
        var expectedAnalytics = new PlatformAnalyticsDto
        {
            NewUsers = new MonthlyMetricDto { Current = 100, Previous = 80, ChangePercent = 25 },
            NewAuctions = new MonthlyMetricDto { Current = 50, Previous = 40, ChangePercent = 25 },
            TotalTransactions = new MonthlyMetricDto { Current = 200, Previous = 150, ChangePercent = 33.33m },
            SuccessRate = new MonthlyMetricDto { Current = 95, Previous = 90, ChangePercent = 5.56m }
        };

        _platformAnalyticsServiceMock.Setup(x => x.GetPlatformAnalyticsAsync())
            .ReturnsAsync(expectedAnalytics);

        // Act
        var result = await _controller.GetPlatformAnalytics();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(expectedAnalytics);
    }

    /// <summary>
    /// Test ID: ANALYTICS-02
    /// Precondition: Service gặp lỗi hệ thống
    /// Input: Không có tham số đầu vào
    /// Condition: Service throw Exception (database error)
    /// Confirmation: HTTP 500 InternalServerError, thông báo Internal server error
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra xử lý exception từ service
    /// </summary>
    [Fact]
    public async Task GetPlatformAnalytics_WithException_ReturnsInternalServerError()
    {
        // Arrange
        _platformAnalyticsServiceMock.Setup(x => x.GetPlatformAnalyticsAsync())
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetPlatformAnalytics();

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
    }

    #endregion

    #region GetAnalyticsDetail

    /// <summary>
    /// Test ID: ANALYTICS-03
    /// Precondition: PlatformAnalyticsService hoạt động bình thường, type hợp lệ
    /// Input: Type hợp lệ ("newUsers", "newAuctions", "totalTransactions", "successRate")
    /// Condition: Lấy chi tiết analytics theo type cụ thể
    /// Confirmation: HTTP 200 OK, trả về object với data chi tiết
    /// Type: Normal
    /// Test Requirement: Kiểm tra chức năng lấy analytics detail thành công
    /// </summary>
    [Fact]
    public async Task GetAnalyticsDetail_WithValidType_ReturnsOk()
    {
        // Arrange
        var expectedDetail = new PlatformAnalyticsDetailDto
        {
            ChartData = new List<ChartDataPoint>
            {
                new ChartDataPoint { Name = "Jan", Value = 1 },
                new ChartDataPoint { Name = "Feb", Value = 2 },
                new ChartDataPoint { Name = "Mar", Value = 3 }
            },
            Summary = new Dictionary<string, object> { ["total"] = 6 }
        };

        _platformAnalyticsServiceMock.Setup(x => x.GetAnalyticsDetailAsync("newUsers"))
            .ReturnsAsync(expectedDetail);

        // Act
        var result = await _controller.GetAnalyticsDetail("newUsers");

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(expectedDetail);
    }

    /// <summary>
    /// Test ID: ANALYTICS-04
    /// Precondition: Service gặp lỗi hệ thống
    /// Input: Type hợp lệ ("newUsers")
    /// Condition: Service throw Exception (database error)
    /// Confirmation: HTTP 500 InternalServerError, thông báo Internal server error
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra xử lý exception từ service
    /// </summary>
    [Fact]
    public async Task GetAnalyticsDetail_WithException_ReturnsInternalServerError()
    {
        // Arrange
        _platformAnalyticsServiceMock.Setup(x => x.GetAnalyticsDetailAsync("newUsers"))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetAnalyticsDetail("newUsers");

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
    }

    #endregion
}

