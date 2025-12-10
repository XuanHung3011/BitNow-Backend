using BitNow_Backend.BLL.IServices;
using BitNow_Backend.Controllers;
using BitNow_Backend.DAL.DTOs;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace BitNow_Backend.Tests.Controllers;

public class SellerStatsControllerTests
{
    private readonly Mock<ISellerStatsService> _sellerStatsServiceMock;
    private readonly Mock<ILogger<SellerStatsController>> _loggerMock;
    private readonly SellerStatsController _controller;

    public SellerStatsControllerTests()
    {
        _sellerStatsServiceMock = new Mock<ISellerStatsService>();
        _loggerMock = new Mock<ILogger<SellerStatsController>>();
        _controller = new SellerStatsController(_sellerStatsServiceMock.Object, _loggerMock.Object);
    }

    #region GetSellerStats

    /// <summary>
    /// Test ID: SELLER-STATS-01
    /// Precondition: Seller tồn tại trong hệ thống, SellerStatsService hoạt động bình thường
    /// Input: SellerId hợp lệ (1)
    /// Condition: Lấy thống kê của seller
    /// Confirmation: HTTP 200 OK, trả về object với sellerId, totalAuctions, totalRevenue, averageRating
    /// Type: Normal
    /// Test Requirement: Kiểm tra chức năng lấy seller stats thành công
    /// </summary>
    [Fact]
    public async Task GetSellerStats_WithValidSellerId_ReturnsOk()
    {
        // Arrange
        var expectedStats = new SellerStatsDto
        {
            ActiveAuctions = 5,
            EndingSoonAuctions = 2,
            TotalListings = 10,
            CompletedAuctions = 8,
            RevenueThisMonth = 5000m,
            RevenueLastMonth = 4000m,
            RevenueChangePercent = 25m,
            TotalBids = 50,
            AverageRating = 4.5m,
            TotalRatings = 20
        };

        _sellerStatsServiceMock.Setup(x => x.GetSellerStatsAsync(1))
            .ReturnsAsync(expectedStats);

        // Act
        var result = await _controller.GetSellerStats(1);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(expectedStats);
    }

    /// <summary>
    /// Test ID: SELLER-STATS-02
    /// Precondition: Service gặp lỗi hệ thống
    /// Input: SellerId hợp lệ (1)
    /// Condition: Service throw Exception (database error)
    /// Confirmation: HTTP 500 InternalServerError, thông báo Internal server error
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra xử lý exception từ service
    /// </summary>
    [Fact]
    public async Task GetSellerStats_WithException_ReturnsInternalServerError()
    {
        // Arrange
        _sellerStatsServiceMock.Setup(x => x.GetSellerStatsAsync(1))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetSellerStats(1);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
    }

    #endregion

    #region GetSellerStatsDetail

    /// <summary>
    /// Test ID: SELLER-STATS-03
    /// Precondition: Seller tồn tại trong hệ thống, SellerStatsService hoạt động bình thường, type hợp lệ
    /// Input: SellerId hợp lệ (1), type hợp lệ ("auctions", "revenue", "bids")
    /// Condition: Lấy chi tiết thống kê của seller theo type cụ thể
    /// Confirmation: HTTP 200 OK, trả về object với data chi tiết
    /// Type: Normal
    /// Test Requirement: Kiểm tra chức năng lấy seller stats detail thành công
    /// </summary>
    [Fact]
    public async Task GetSellerStatsDetail_WithValidParams_ReturnsOk()
    {
        // Arrange
        var expectedDetail = new SellerStatsDetailDto
        {
            ChartData = new List<ChartDataPoint>
            {
                new ChartDataPoint { Name = "Jan", Value = 1 },
                new ChartDataPoint { Name = "Feb", Value = 2 },
                new ChartDataPoint { Name = "Mar", Value = 3 }
            },
            Summary = new Dictionary<string, object> { ["total"] = 6 }
        };

        _sellerStatsServiceMock.Setup(x => x.GetSellerStatsDetailAsync(1, "auctions"))
            .ReturnsAsync(expectedDetail);

        // Act
        var result = await _controller.GetSellerStatsDetail(1, "auctions");

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(expectedDetail);
    }

    /// <summary>
    /// Test ID: SELLER-STATS-04
    /// Precondition: Service gặp lỗi hệ thống
    /// Input: SellerId hợp lệ (1), type hợp lệ ("auctions")
    /// Condition: Service throw Exception (database error)
    /// Confirmation: HTTP 500 InternalServerError, thông báo Internal server error
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra xử lý exception từ service
    /// </summary>
    [Fact]
    public async Task GetSellerStatsDetail_WithException_ReturnsInternalServerError()
    {
        // Arrange
        _sellerStatsServiceMock.Setup(x => x.GetSellerStatsDetailAsync(1, "auctions"))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetSellerStatsDetail(1, "auctions");

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
    }

    #endregion
}

