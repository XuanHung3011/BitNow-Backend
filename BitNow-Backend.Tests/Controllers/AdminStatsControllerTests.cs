using BitNow_Backend.BLL.IServices;
using BitNow_Backend.Controllers;
using BitNow_Backend.DAL.DTOs;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace BitNow_Backend.Tests.Controllers;

public class AdminStatsControllerTests
{
    private readonly Mock<IAdminStatsService> _adminStatsServiceMock;
    private readonly Mock<ILogger<AdminStatsController>> _loggerMock;
    private readonly AdminStatsController _controller;

    public AdminStatsControllerTests()
    {
        _adminStatsServiceMock = new Mock<IAdminStatsService>();
        _loggerMock = new Mock<ILogger<AdminStatsController>>();
        _controller = new AdminStatsController(_adminStatsServiceMock.Object, _loggerMock.Object);
    }

    #region GetAdminStats

    [Fact]
    public async Task GetAdminStats_WithValidData_ReturnsOk()
    {
        // Arrange
        var stats = new AdminStatsDto
        {
            TotalUsers = 100,
            NewUsersThisWeek = 10,
            ActiveAuctions = 5,
            PendingItems = 3,
            DisputesProcessing = 2,
            UrgentDisputes = 1,
            RevenueThisMonth = 1000,
            RevenueLastMonth = 800,
            RevenueChangePercent = 25
        };

        _adminStatsServiceMock.Setup(x => x.GetAdminStatsAsync())
            .ReturnsAsync(stats);

        // Act
        var result = await _controller.GetAdminStats();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(stats);
        _adminStatsServiceMock.Verify(x => x.GetAdminStatsAsync(), Times.Once);
    }

    [Fact]
    public async Task GetAdminStats_WithException_ReturnsInternalServerError()
    {
        // Arrange
        _adminStatsServiceMock.Setup(x => x.GetAdminStatsAsync())
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetAdminStats();

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
    }

    #endregion

    #region GetAdminStatsDetail

    [Fact]
    public async Task GetAdminStatsDetail_WithValidType_ReturnsOk()
    {
        // Arrange
        var type = "users";
        var detail = new AdminStatsDetailDto
        {
            ChartData = new List<ChartDataPoint>
            {
                new() { Name = "Jan", Value = 10 },
                new() { Name = "Feb", Value = 20 }
            },
            Summary = new Dictionary<string, object>
            {
                ["type"] = type,
                ["total"] = 30
            }
        };

        _adminStatsServiceMock.Setup(x => x.GetAdminStatsDetailAsync(type))
            .ReturnsAsync(detail);

        // Act
        var result = await _controller.GetAdminStatsDetail(type);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(detail);
        _adminStatsServiceMock.Verify(x => x.GetAdminStatsDetailAsync(type), Times.Once);
    }

    [Fact]
    public async Task GetAdminStatsDetail_WithException_ReturnsInternalServerError()
    {
        // Arrange
        var type = "revenue";
        _adminStatsServiceMock.Setup(x => x.GetAdminStatsDetailAsync(type))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetAdminStatsDetail(type);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
    }

    #endregion
}


