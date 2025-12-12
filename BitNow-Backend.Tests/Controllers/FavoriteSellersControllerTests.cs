using BitNow_Backend.BLL.IServices;
using BitNow_Backend.Controllers;
using BitNow_Backend.DAL.DTOs;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace BitNow_Backend.Tests.Controllers;

public class FavoriteSellersControllerTests
{
    private readonly Mock<IFavoriteSellerService> _serviceMock;
    private readonly FavoriteSellersController _controller;

    public FavoriteSellersControllerTests()
    {
        _serviceMock = new Mock<IFavoriteSellerService>();
        _controller = new FavoriteSellersController(_serviceMock.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    private void SetUserHeader(int? userId)
    {
        if (userId.HasValue)
        {
            _controller.HttpContext.Request.Headers["X-User-Id"] = userId.Value.ToString();
        }
        else
        {
            _controller.HttpContext.Request.Headers.Remove("X-User-Id");
        }
    }

    #region GetMyFavorites

    /// <summary>
    /// Test ID: FAV-01
    /// Precondition: User đã đăng nhập (có X-User-Id header), FavoriteSellerService hoạt động bình thường
    /// Input: X-User-Id header hợp lệ (10)
    /// Condition: Lấy danh sách favorite sellers của user đã đăng nhập
    /// Confirmation: HTTP 200 OK, trả về danh sách FavoriteSellerDto
    /// Type: Normal
    /// Test Requirement: Kiểm tra chức năng lấy danh sách favorite sellers thành công
    /// </summary>
    [Fact]
    public async Task GetMyFavorites_WithUserHeader_ReturnsOk()
    {
        // Arrange
        SetUserHeader(10);
        var favorites = new List<FavoriteSellerDto>
        {
            new() { Id = 1, BuyerId = 10, SellerId = 20, SellerName = "Seller A" }
        };
        _serviceMock.Setup(x => x.GetFavoritesAsync(10)).ReturnsAsync(favorites);

        // Act
        var result = await _controller.GetMyFavorites();

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var ok = result.Result as OkObjectResult;
        ok!.Value.Should().BeEquivalentTo(favorites);
    }

    /// <summary>
    /// Test ID: FAV-02
    /// Precondition: User chưa đăng nhập (không có X-User-Id header)
    /// Input: Không có X-User-Id header
    /// Condition: Lấy danh sách favorite sellers khi chưa đăng nhập
    /// Confirmation: HTTP 401 Unauthorized, thông báo Vui lòng đăng nhập
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra xử lý trường hợp chưa đăng nhập
    /// </summary>
    [Fact]
    public async Task GetMyFavorites_WithoutUserHeader_ReturnsUnauthorized()
    {
        // Arrange
        SetUserHeader(null);

        // Act
        var result = await _controller.GetMyFavorites();

        // Assert
        result.Result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    #endregion

    #region CheckIsFavorite

    /// <summary>
    /// Test ID: FAV-03
    /// Precondition: User đã đăng nhập (có X-User-Id header), Seller tồn tại, FavoriteSellerService hoạt động bình thường
    /// Input: X-User-Id header hợp lệ (10), SellerId hợp lệ (30)
    /// Condition: Kiểm tra seller có trong danh sách favorite của user không
    /// Confirmation: HTTP 200 OK, trả về { isFavorite = true }
    /// Type: Normal
    /// Test Requirement: Kiểm tra chức năng kiểm tra favorite seller thành công
    /// </summary>
    [Fact]
    public async Task CheckIsFavorite_WithUserHeader_ReturnsOk()
    {
        // Arrange
        SetUserHeader(10);
        _serviceMock.Setup(x => x.IsFavoriteAsync(10, 30)).ReturnsAsync(true);

        // Act
        var result = await _controller.CheckIsFavorite(30);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var ok = result.Result as OkObjectResult;
        ok!.Value.Should().BeEquivalentTo(new { isFavorite = true });
    }

    /// <summary>
    /// Test ID: FAV-04
    /// Precondition: User chưa đăng nhập (không có X-User-Id header)
    /// Input: Không có X-User-Id header, SellerId hợp lệ (30)
    /// Condition: Kiểm tra favorite khi chưa đăng nhập
    /// Confirmation: HTTP 200 OK, trả về { isFavorite = false } (không throw Unauthorized)
    /// Type: Boundary
    /// Test Requirement: Kiểm tra xử lý trường hợp chưa đăng nhập (trả về false thay vì Unauthorized)
    /// </summary>
    [Fact]
    public async Task CheckIsFavorite_WithoutUserHeader_ReturnsOkFalse()
    {
        // Arrange
        SetUserHeader(null);

        // Act
        var result = await _controller.CheckIsFavorite(30);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var ok = result.Result as OkObjectResult;
        ok!.Value.Should().BeEquivalentTo(new { isFavorite = false });
    }

    #endregion

    #region AddFavorite

    /// <summary>
    /// Test ID: FAV-05
    /// Precondition: User đã đăng nhập (có X-User-Id header), Seller tồn tại và chưa trong favorite list, FavoriteSellerService hoạt động bình thường
    /// Input: X-User-Id header hợp lệ (10), AddFavoriteSellerDto hợp lệ (SellerId=20)
    /// Condition: Thêm seller vào danh sách favorite của user
    /// Confirmation: HTTP 200 OK, trả về FavoriteSellerResponseDto với Success=true
    /// Type: Normal
    /// Test Requirement: Kiểm tra chức năng thêm favorite seller thành công
    /// </summary>
    [Fact]
    public async Task AddFavorite_WithUserHeader_Success_ReturnsOk()
    {
        // Arrange
        SetUserHeader(10);
        var dto = new AddFavoriteSellerDto { SellerId = 20 };
        var response = new FavoriteSellerResponseDto { Success = true, Message = "ok" };
        _serviceMock.Setup(x => x.AddFavoriteAsync(10, dto.SellerId)).ReturnsAsync(response);

        // Act
        var result = await _controller.AddFavorite(dto);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var ok = result.Result as OkObjectResult;
        ok!.Value.Should().BeEquivalentTo(response);
    }

    /// <summary>
    /// Test ID: FAV-06
    /// Precondition: User đã đăng nhập, Seller đã có trong favorite list hoặc không tồn tại
    /// Input: X-User-Id header hợp lệ (10), AddFavoriteSellerDto hợp lệ (SellerId=20)
    /// Condition: Thêm seller đã tồn tại hoặc không hợp lệ vào favorite list
    /// Confirmation: HTTP 400 BadRequest, trả về FavoriteSellerResponseDto với Success=false
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra xử lý trường hợp thêm favorite seller thất bại (duplicate hoặc không hợp lệ)
    /// </summary>
    [Fact]
    public async Task AddFavorite_WithUserHeader_Fails_ReturnsBadRequest()
    {
        // Arrange
        SetUserHeader(10);
        var dto = new AddFavoriteSellerDto { SellerId = 20 };
        var response = new FavoriteSellerResponseDto { Success = false, Message = "duplicate" };
        _serviceMock.Setup(x => x.AddFavoriteAsync(10, dto.SellerId)).ReturnsAsync(response);

        // Act
        var result = await _controller.AddFavorite(dto);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    /// <summary>
    /// Test ID: FAV-07
    /// Precondition: User chưa đăng nhập (không có X-User-Id header)
    /// Input: Không có X-User-Id header, AddFavoriteSellerDto hợp lệ
    /// Condition: Thêm favorite seller khi chưa đăng nhập
    /// Confirmation: HTTP 401 Unauthorized, thông báo Vui lòng đăng nhập
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra xử lý trường hợp chưa đăng nhập
    /// </summary>
    [Fact]
    public async Task AddFavorite_WithoutUserHeader_ReturnsUnauthorized()
    {
        // Arrange
        SetUserHeader(null);

        // Act
        var result = await _controller.AddFavorite(new AddFavoriteSellerDto { SellerId = 20 });

        // Assert
        result.Result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    #endregion

    #region RemoveFavorite

    /// <summary>
    /// Test ID: FAV-08
    /// Precondition: User đã đăng nhập (có X-User-Id header), Seller tồn tại trong favorite list, FavoriteSellerService hoạt động bình thường
    /// Input: X-User-Id header hợp lệ (10), SellerId hợp lệ (30)
    /// Condition: Xóa seller khỏi danh sách favorite của user
    /// Confirmation: HTTP 200 OK, trả về FavoriteSellerResponseDto với Success=true, Message="removed"
    /// Type: Normal
    /// Test Requirement: Kiểm tra chức năng xóa favorite seller thành công
    /// </summary>
    [Fact]
    public async Task RemoveFavorite_WithUserHeader_Success_ReturnsOk()
    {
        // Arrange
        SetUserHeader(10);
        var response = new FavoriteSellerResponseDto { Success = true, Message = "removed" };
        _serviceMock.Setup(x => x.RemoveFavoriteAsync(10, 30)).ReturnsAsync(response);

        // Act
        var result = await _controller.RemoveFavorite(30);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var ok = result.Result as OkObjectResult;
        ok!.Value.Should().BeEquivalentTo(response);
    }

    /// <summary>
    /// Test ID: FAV-09
    /// Precondition: User đã đăng nhập, Seller không tồn tại trong favorite list
    /// Input: X-User-Id header hợp lệ (10), SellerId không tồn tại trong favorite (30)
    /// Condition: Xóa seller không tồn tại khỏi favorite list
    /// Confirmation: HTTP 404 NotFound, trả về FavoriteSellerResponseDto với Success=false, Message="not found"
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra xử lý trường hợp seller không tồn tại trong favorite list
    /// </summary>
    [Fact]
    public async Task RemoveFavorite_WithUserHeader_NotFound_ReturnsNotFound()
    {
        // Arrange
        SetUserHeader(10);
        var response = new FavoriteSellerResponseDto { Success = false, Message = "not found" };
        _serviceMock.Setup(x => x.RemoveFavoriteAsync(10, 30)).ReturnsAsync(response);

        // Act
        var result = await _controller.RemoveFavorite(30);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    /// <summary>
    /// Test ID: FAV-10
    /// Precondition: User chưa đăng nhập (không có X-User-Id header)
    /// Input: Không có X-User-Id header, SellerId hợp lệ (30)
    /// Condition: Xóa favorite seller khi chưa đăng nhập
    /// Confirmation: HTTP 401 Unauthorized, thông báo Vui lòng đăng nhập
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra xử lý trường hợp chưa đăng nhập
    /// </summary>
    [Fact]
    public async Task RemoveFavorite_WithoutUserHeader_ReturnsUnauthorized()
    {
        // Arrange
        SetUserHeader(null);

        // Act
        var result = await _controller.RemoveFavorite(30);

        // Assert
        result.Result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    #endregion
}

