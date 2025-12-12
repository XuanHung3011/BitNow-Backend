using BitNow_Backend.BLL.IServices;
using BitNow_Backend.Controllers;
using BitNow_Backend.DAL.DTOs;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace BitNow_Backend.Tests.Controllers;

public class WatchlistControllerTests
{
    private readonly Mock<IWatchlistService> _serviceMock;
    private readonly WatchlistController _controller;

    public WatchlistControllerTests()
    {
        _serviceMock = new Mock<IWatchlistService>();
        _controller = new WatchlistController(_serviceMock.Object);
    }

    #region AddWatchList

    /// <summary>
    /// Test ID: WATCH-01
    /// Precondition: User tồn tại, Auction tồn tại, WatchlistService hoạt động bình thường, auction chưa có trong watchlist
    /// Input: AddToWatchlistRequest hợp lệ (UserId=1, AuctionId=2)
    /// Condition: Thêm auction vào watchlist của user
    /// Confirmation: HTTP 200 OK, thông báo Đã thêm sản phẩm vào danh sách theo dõi
    /// Type: Normal
    /// Test Requirement: Kiểm tra chức năng thêm auction vào watchlist thành công
    /// </summary>
    [Fact]
    public async Task AddWatchList_WithValidRequest_ReturnsOk()
    {
        // Arrange
        var request = new AddToWatchlistRequest { UserId = 1, AuctionId = 2 };
        _serviceMock.Setup(x => x.AddAsync(request)).ReturnsAsync(true);

        // Act
        var result = await _controller.AddWatchList(request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    /// <summary>
    /// Test ID: WATCH-02
    /// Precondition: ModelState không hợp lệ
    /// Input: AddToWatchlistRequest với UserId thiếu hoặc không hợp lệ
    /// Condition: Thêm watchlist với dữ liệu không hợp lệ theo validation rules
    /// Confirmation: HTTP 400 BadRequest, không gọi AddAsync
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra validation model state trước khi xử lý
    /// </summary>
    [Fact]
    public async Task AddWatchList_WithInvalidModel_ReturnsBadRequest()
    {
        // Arrange
        _controller.ModelState.AddModelError("UserId", "Required");

        // Act
        var result = await _controller.AddWatchList(new AddToWatchlistRequest());

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    /// <summary>
    /// Test ID: WATCH-03
    /// Precondition: Auction đã có trong watchlist hoặc không thể thêm
    /// Input: AddToWatchlistRequest hợp lệ (UserId=1, AuctionId=2)
    /// Condition: Thêm auction đã tồn tại hoặc không hợp lệ vào watchlist
    /// Confirmation: HTTP 400 BadRequest, thông báo Failed to add
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra xử lý trường hợp thêm watchlist thất bại
    /// </summary>
    [Fact]
    public async Task AddWatchList_WhenServiceReturnsFalse_ReturnsBadRequest()
    {
        // Arrange
        var request = new AddToWatchlistRequest { UserId = 1, AuctionId = 2 };
        _serviceMock.Setup(x => x.AddAsync(request)).ReturnsAsync(false);

        // Act
        var result = await _controller.AddWatchList(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region RemoveWatchList

    /// <summary>
    /// Test ID: WATCH-04
    /// Precondition: Auction tồn tại trong watchlist của user, WatchlistService hoạt động bình thường
    /// Input: RemoveFromWatchlistRequest hợp lệ (UserId=1, AuctionId=2)
    /// Condition: Xóa auction khỏi watchlist của user
    /// Confirmation: HTTP 200 OK, thông báo Đã xóa sản phẩm khỏi danh sách theo dõi
    /// Type: Normal
    /// Test Requirement: Kiểm tra chức năng xóa auction khỏi watchlist thành công
    /// </summary>
    [Fact]
    public async Task RemoveWatchList_WithValidRequest_ReturnsOk()
    {
        // Arrange
        var request = new RemoveFromWatchlistRequest { UserId = 1, AuctionId = 2 };
        _serviceMock.Setup(x => x.RemoveAsync(request)).ReturnsAsync(true);

        // Act
        var result = await _controller.RemoveWatchList(request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    /// <summary>
    /// Test ID: WATCH-05
    /// Precondition: ModelState không hợp lệ
    /// Input: RemoveFromWatchlistRequest với AuctionId thiếu hoặc không hợp lệ
    /// Condition: Xóa watchlist với dữ liệu không hợp lệ theo validation rules
    /// Confirmation: HTTP 400 BadRequest, không gọi RemoveAsync
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra validation model state trước khi xử lý
    /// </summary>
    [Fact]
    public async Task RemoveWatchList_WithInvalidModel_ReturnsBadRequest()
    {
        // Arrange
        _controller.ModelState.AddModelError("AuctionId", "Required");

        // Act
        var result = await _controller.RemoveWatchList(new RemoveFromWatchlistRequest());

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    /// <summary>
    /// Test ID: WATCH-06
    /// Precondition: Auction không tồn tại trong watchlist của user
    /// Input: RemoveFromWatchlistRequest hợp lệ (UserId=1, AuctionId=2)
    /// Condition: Xóa auction không tồn tại khỏi watchlist
    /// Confirmation: HTTP 404 NotFound, thông báo Not found
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra xử lý trường hợp auction không tồn tại trong watchlist
    /// </summary>
    [Fact]
    public async Task RemoveWatchList_WhenServiceReturnsFalse_ReturnsNotFound()
    {
        // Arrange
        var request = new RemoveFromWatchlistRequest { UserId = 1, AuctionId = 2 };
        _serviceMock.Setup(x => x.RemoveAsync(request)).ReturnsAsync(false);

        // Act
        var result = await _controller.RemoveWatchList(request);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region GetByUser

    /// <summary>
    /// Test ID: WATCH-07
    /// Precondition: User tồn tại trong hệ thống, có items trong watchlist, WatchlistService hoạt động bình thường
    /// Input: UserId hợp lệ (1)
    /// Condition: Lấy danh sách watchlist items của user
    /// Confirmation: HTTP 200 OK, trả về danh sách WatchlistItemDto
    /// Type: Normal
    /// Test Requirement: Kiểm tra chức năng lấy watchlist items của user thành công
    /// </summary>
    [Fact]
    public async Task GetByUser_WithExistingItems_ReturnsOk()
    {
        // Arrange
        var items = new List<WatchlistItemDto>
        {
            new() { WatchlistId = 1, UserId = 1, AuctionId = 2, ItemTitle = "Item 1", Status = "active" }
        };
        _serviceMock.Setup(x => x.GetByUserAsync(1)).ReturnsAsync(items);

        // Act
        var result = await _controller.GetByUser(1);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var ok = result.Result as OkObjectResult;
        ok!.Value.Should().BeEquivalentTo(items);
    }

    #endregion

    #region GetDetail

    /// <summary>
    /// Test ID: WATCH-08
    /// Precondition: Watchlist record tồn tại trong hệ thống, WatchlistService hoạt động bình thường
    /// Input: WatchlistId hợp lệ (5)
    /// Condition: Lấy chi tiết watchlist item theo ID
    /// Confirmation: HTTP 200 OK, trả về WatchlistItemDto với WatchlistId=5
    /// Type: Normal
    /// Test Requirement: Kiểm tra chức năng lấy chi tiết watchlist item thành công
    /// </summary>
    [Fact]
    public async Task GetDetail_WithExistingRecord_ReturnsOk()
    {
        // Arrange
        var item = new WatchlistItemDto { WatchlistId = 5, UserId = 1, AuctionId = 2, ItemTitle = "Item 1", Status = "active" };
        _serviceMock.Setup(x => x.GetDetailAsync(5)).ReturnsAsync(item);

        // Act
        var result = await _controller.GetDetail(5);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var ok = result.Result as OkObjectResult;
        ok!.Value.Should().BeEquivalentTo(item);
    }

    /// <summary>
    /// Test ID: WATCH-09
    /// Precondition: Watchlist record không tồn tại trong hệ thống
    /// Input: WatchlistId không tồn tại (5)
    /// Condition: Lấy chi tiết watchlist item không tồn tại
    /// Confirmation: HTTP 404 NotFound
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra xử lý trường hợp watchlist item không tồn tại
    /// </summary>
    [Fact]
    public async Task GetDetail_WithNonExistingRecord_ReturnsNotFound()
    {
        // Arrange
        _serviceMock.Setup(x => x.GetDetailAsync(5)).ReturnsAsync((WatchlistItemDto?)null);

        // Act
        var result = await _controller.GetDetail(5);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    #endregion

    #region GetDetailByUserAuction

    /// <summary>
    /// Test ID: WATCH-10
    /// Precondition: Watchlist record tồn tại cho user và auction cụ thể, WatchlistService hoạt động bình thường
    /// Input: UserId hợp lệ (1), AuctionId hợp lệ (3)
    /// Condition: Lấy chi tiết watchlist item theo user và auction
    /// Confirmation: HTTP 200 OK, trả về WatchlistItemDto với UserId=1, AuctionId=3
    /// Type: Normal
    /// Test Requirement: Kiểm tra chức năng lấy chi tiết watchlist item theo user và auction thành công
    /// </summary>
    [Fact]
    public async Task GetDetailByUserAuction_WithExistingRecord_ReturnsOk()
    {
        // Arrange
        var item = new WatchlistItemDto { WatchlistId = 6, UserId = 1, AuctionId = 3, ItemTitle = "Item 2", Status = "active" };
        _serviceMock.Setup(x => x.GetDetailByUserAuctionAsync(1, 3)).ReturnsAsync(item);

        // Act
        var result = await _controller.GetDetailByUserAuction(1, 3);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var ok = result.Result as OkObjectResult;
        ok!.Value.Should().BeEquivalentTo(item);
    }

    /// <summary>
    /// Test ID: WATCH-11
    /// Precondition: Watchlist record không tồn tại cho user và auction cụ thể
    /// Input: UserId hợp lệ (1), AuctionId hợp lệ (3) nhưng không có watchlist record
    /// Condition: Lấy chi tiết watchlist item không tồn tại
    /// Confirmation: HTTP 404 NotFound
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra xử lý trường hợp watchlist item không tồn tại cho user và auction
    /// </summary>
    [Fact]
    public async Task GetDetailByUserAuction_WithNonExistingRecord_ReturnsNotFound()
    {
        // Arrange
        _serviceMock.Setup(x => x.GetDetailByUserAuctionAsync(1, 3)).ReturnsAsync((WatchlistItemDto?)null);

        // Act
        var result = await _controller.GetDetailByUserAuction(1, 3);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    #endregion
}

