using BitNow_Backend.BLL.IServices;
using BitNow_Backend.Controllers;
using BitNow_Backend.DAL.DTOs;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace BitNow_Backend.Tests.Controllers;

public class HomeControllerTests
{
    private readonly Mock<IItemService> _itemServiceMock;
    private readonly Mock<ISearchKeywordService> _searchKeywordServiceMock;
    private readonly Mock<ILogger<HomeController>> _loggerMock;
    private readonly HomeController _controller;

    public HomeControllerTests()
    {
        _itemServiceMock = new Mock<IItemService>();
        _searchKeywordServiceMock = new Mock<ISearchKeywordService>();
        _loggerMock = new Mock<ILogger<HomeController>>();
        _controller = new HomeController(_itemServiceMock.Object, _searchKeywordServiceMock.Object, _loggerMock.Object);
    }

    #region GetAllItems

    /// <summary>
    /// Test ID: HOME-01
    /// Precondition: ItemService hoạt động bình thường, có approved items trong hệ thống
    /// Input: Không có tham số đầu vào
    /// Condition: Lấy tất cả approved items từ hệ thống
    /// Confirmation: HTTP 200 OK, trả về danh sách ItemResponseDto với Status="approved"
    /// Type: Normal
    /// Test Requirement: Kiểm tra chức năng lấy tất cả approved items thành công
    /// </summary>
    [Fact]
    public async Task GetAllItems_WithValidData_ReturnsOk()
    {
        // Arrange
        var expectedItems = new List<ItemResponseDto>
        {
            new()
            {
                Id = 1,
                Title = "Test Item",
                Status = "approved"
            }
        };

        _itemServiceMock.Setup(x => x.GetAllApprovedItemsAsync())
            .ReturnsAsync(expectedItems);

        // Act
        var result = await _controller.GetAllItems();

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(expectedItems);
    }

    /// <summary>
    /// Test ID: HOME-02
    /// Precondition: Service gặp lỗi hệ thống
    /// Input: Không có tham số đầu vào
    /// Condition: Service throw Exception (database error)
    /// Confirmation: HTTP 500 InternalServerError, thông báo Internal server error
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra xử lý exception từ service
    /// </summary>
    [Fact]
    public async Task GetAllItems_WithException_ReturnsInternalServerError()
    {
        // Arrange
        _itemServiceMock.Setup(x => x.GetAllApprovedItemsAsync())
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetAllItems();

        // Assert
        result.Result.Should().BeOfType<ObjectResult>();
        var objectResult = result.Result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
    }

    #endregion

    #region GetAllItemsPaged

    /// <summary>
    /// Test ID: HOME-03
    /// Precondition: ItemService hoạt động bình thường, có approved items trong hệ thống
    /// Input: page hợp lệ (1), pageSize hợp lệ (10)
    /// Condition: Lấy approved items có phân trang với tham số hợp lệ
    /// Confirmation: HTTP 200 OK, trả về object với items và pagination info
    /// Type: Normal
    /// Test Requirement: Kiểm tra chức năng lấy approved items có phân trang thành công
    /// </summary>
    [Fact]
    public async Task GetAllItemsPaged_WithValidParams_ReturnsOk()
    {
        // Arrange
        var expectedItems = new List<ItemResponseDto>
        {
            new() { Id = 1, Title = "Item 1" }
        };
        var totalCount = 10;

        _itemServiceMock.Setup(x => x.GetAllApprovedItemsWithCountAsync(1, 10))
            .ReturnsAsync((expectedItems, totalCount));

        // Act
        var result = await _controller.GetAllItemsPaged(1, 10);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    /// <summary>
    /// Test ID: HOME-04
    /// Precondition: ItemService hoạt động bình thường
    /// Input: page = 0 (giá trị biên không hợp lệ), pageSize hợp lệ (10)
    /// Condition: Lấy items với page < 1, hệ thống tự động điều chỉnh về page = 1
    /// Confirmation: HTTP 200 OK, gọi service với page = 1
    /// Type: Boundary
    /// Test Requirement: Kiểm tra validation và điều chỉnh page < 1 về page = 1
    /// </summary>
    [Fact]
    public async Task GetAllItemsPaged_WithPageLessThanOne_AdjustsToPageOne()
    {
        // Arrange
        var expectedItems = new List<ItemResponseDto>();
        var totalCount = 0;

        _itemServiceMock.Setup(x => x.GetAllApprovedItemsWithCountAsync(1, 10))
            .ReturnsAsync((expectedItems, totalCount));

        // Act
        var result = await _controller.GetAllItemsPaged(0, 10);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        _itemServiceMock.Verify(x => x.GetAllApprovedItemsWithCountAsync(1, 10), Times.Once);
    }

    /// <summary>
    /// Test ID: HOME-05
    /// Precondition: ItemService hoạt động bình thường
    /// Input: page hợp lệ (1), pageSize = 200 (giá trị biên vượt quá max)
    /// Condition: Lấy items với pageSize > 100, hệ thống tự động điều chỉnh về pageSize = 100
    /// Confirmation: HTTP 200 OK, gọi service với pageSize = 100
    /// Type: Boundary
    /// Test Requirement: Kiểm tra validation và điều chỉnh pageSize > 100 về pageSize = 100
    /// </summary>
    [Fact]
    public async Task GetAllItemsPaged_WithPageSizeGreaterThan100_AdjustsTo100()
    {
        // Arrange
        var expectedItems = new List<ItemResponseDto>();
        var totalCount = 0;

        _itemServiceMock.Setup(x => x.GetAllApprovedItemsWithCountAsync(1, 100))
            .ReturnsAsync((expectedItems, totalCount));

        // Act
        var result = await _controller.GetAllItemsPaged(1, 200);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        _itemServiceMock.Verify(x => x.GetAllApprovedItemsWithCountAsync(1, 100), Times.Once);
    }

    /// <summary>
    /// Test ID: HOME-06
    /// Precondition: Service gặp lỗi hệ thống
    /// Input: page hợp lệ (1), pageSize hợp lệ (10)
    /// Condition: Service throw Exception (database error)
    /// Confirmation: HTTP 500 InternalServerError, thông báo Internal server error
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra xử lý exception từ service
    /// </summary>
    [Fact]
    public async Task GetAllItemsPaged_WithException_ReturnsInternalServerError()
    {
        // Arrange
        _itemServiceMock.Setup(x => x.GetAllApprovedItemsWithCountAsync(1, 10))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetAllItemsPaged(1, 10);

        // Assert
        result.Result.Should().BeOfType<ObjectResult>();
        var objectResult = result.Result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
    }

    #endregion

    #region SearchItems

    /// <summary>
    /// Test ID: HOME-07
    /// Precondition: ItemService hoạt động bình thường, có approved items khớp với search term
    /// Input: searchTerm hợp lệ ("test")
    /// Condition: Tìm kiếm approved items với search term hợp lệ
    /// Confirmation: HTTP 200 OK, trả về danh sách ItemResponseDto khớp với search term
    /// Type: Normal
    /// Test Requirement: Kiểm tra chức năng tìm kiếm approved items thành công
    /// </summary>
    [Fact]
    public async Task SearchItems_WithValidSearchTerm_ReturnsOk()
    {
        // Arrange
        var expectedItems = new List<ItemResponseDto>
        {
            new() { Id = 1, Title = "Test Item" }
        };

        _itemServiceMock.Setup(x => x.SearchApprovedItemsAsync("test"))
            .ReturnsAsync(expectedItems);

        // Act
        var result = await _controller.SearchItems("test");

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(expectedItems);
    }

    /// <summary>
    /// Test ID: HOME-08
    /// Precondition: searchTerm không hợp lệ
    /// Input: searchTerm rỗng ("")
    /// Condition: Tìm kiếm với search term rỗng
    /// Confirmation: HTTP 400 BadRequest, thông báo Search term is required
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra validation search term không được rỗng
    /// </summary>
    [Fact]
    public async Task SearchItems_WithEmptySearchTerm_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.SearchItems("");

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    /// <summary>
    /// Test ID: HOME-09
    /// Precondition: searchTerm không hợp lệ
    /// Input: searchTerm chỉ có khoảng trắng ("   ")
    /// Condition: Tìm kiếm với search term chỉ có whitespace
    /// Confirmation: HTTP 400 BadRequest, thông báo Search term is required
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra validation search term không được chỉ có whitespace
    /// </summary>
    [Fact]
    public async Task SearchItems_WithWhitespaceSearchTerm_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.SearchItems("   ");

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    /// <summary>
    /// Test ID: HOME-10
    /// Precondition: Service gặp lỗi hệ thống
    /// Input: searchTerm hợp lệ ("test")
    /// Condition: Service throw Exception (database error)
    /// Confirmation: HTTP 500 InternalServerError, thông báo Internal server error
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra xử lý exception từ service
    /// </summary>
    [Fact]
    public async Task SearchItems_WithException_ReturnsInternalServerError()
    {
        // Arrange
        _itemServiceMock.Setup(x => x.SearchApprovedItemsAsync("test"))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.SearchItems("test");

        // Assert
        result.Result.Should().BeOfType<ObjectResult>();
        var objectResult = result.Result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
    }

    #endregion

    #region SearchItemsPaged

    /// <summary>
    /// Test ID: HOME-11
    /// Precondition: ItemService hoạt động bình thường, có approved items khớp với search term
    /// Input: searchTerm hợp lệ ("test"), page hợp lệ (1), pageSize hợp lệ (10)
    /// Condition: Tìm kiếm approved items có phân trang với tham số hợp lệ
    /// Confirmation: HTTP 200 OK, trả về object với items và pagination info
    /// Type: Normal
    /// Test Requirement: Kiểm tra chức năng tìm kiếm approved items có phân trang thành công
    /// </summary>
    [Fact]
    public async Task SearchItemsPaged_WithValidParams_ReturnsOk()
    {
        // Arrange
        var expectedItems = new List<ItemResponseDto>
        {
            new() { Id = 1, Title = "Test Item" }
        };
        var totalCount = 5;

        _itemServiceMock.Setup(x => x.SearchApprovedItemsWithCountAsync("test", 1, 10))
            .ReturnsAsync((expectedItems, totalCount));

        // Act
        var result = await _controller.SearchItemsPaged("test", 1, 10);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    /// <summary>
    /// Test ID: HOME-12
    /// Precondition: searchTerm không hợp lệ
    /// Input: searchTerm rỗng (""), page hợp lệ (1), pageSize hợp lệ (10)
    /// Condition: Tìm kiếm có phân trang với search term rỗng
    /// Confirmation: HTTP 400 BadRequest, thông báo Search term is required
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra validation search term không được rỗng khi có phân trang
    /// </summary>
    [Fact]
    public async Task SearchItemsPaged_WithEmptySearchTerm_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.SearchItemsPaged("", 1, 10);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    /// <summary>
    /// Test ID: HOME-13
    /// Precondition: Service gặp lỗi hệ thống
    /// Input: searchTerm hợp lệ ("test"), page hợp lệ (1), pageSize hợp lệ (10)
    /// Condition: Service throw Exception (database error)
    /// Confirmation: HTTP 500 InternalServerError, thông báo Internal server error
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra xử lý exception từ service
    /// </summary>
    [Fact]
    public async Task SearchItemsPaged_WithException_ReturnsInternalServerError()
    {
        // Arrange
        _itemServiceMock.Setup(x => x.SearchApprovedItemsWithCountAsync("test", 1, 10))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.SearchItemsPaged("test", 1, 10);

        // Assert
        result.Result.Should().BeOfType<ObjectResult>();
        var objectResult = result.Result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
    }

    #endregion

    #region FilterItems

    /// <summary>
    /// Test ID: HOME-14
    /// Precondition: ItemService hoạt động bình thường, có approved items khớp với filter
    /// Input: ItemFilterDto hợp lệ, page hợp lệ (1), pageSize hợp lệ (10)
    /// Condition: Lọc approved items với filter hợp lệ và phân trang
    /// Confirmation: HTTP 200 OK, trả về object với items và pagination info
    /// Type: Normal
    /// Test Requirement: Kiểm tra chức năng lọc approved items thành công
    /// </summary>
    [Fact]
    public async Task FilterItems_WithValidFilter_ReturnsOk()
    {
        // Arrange
        var filter = new ItemFilterDto();
        var expectedItems = new List<ItemResponseDto>
        {
            new() { Id = 1, Title = "Test Item" }
        };
        var totalCount = 5;

        _itemServiceMock.Setup(x => x.FilterApprovedItemsAsync(filter, 1, 10))
            .ReturnsAsync((expectedItems, totalCount));

        // Act
        var result = await _controller.FilterItems(filter, 1, 10);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    /// <summary>
    /// Test ID: HOME-15
    /// Precondition: Service gặp lỗi hệ thống
    /// Input: ItemFilterDto hợp lệ, page hợp lệ (1), pageSize hợp lệ (10)
    /// Condition: Service throw Exception (database error)
    /// Confirmation: HTTP 500 InternalServerError, thông báo Internal server error
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra xử lý exception từ service
    /// </summary>
    [Fact]
    public async Task FilterItems_WithException_ReturnsInternalServerError()
    {
        // Arrange
        var filter = new ItemFilterDto();
        _itemServiceMock.Setup(x => x.FilterApprovedItemsAsync(filter, 1, 10))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.FilterItems(filter, 1, 10);

        // Assert
        result.Result.Should().BeOfType<ObjectResult>();
        var objectResult = result.Result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
    }

    #endregion

    #region GetCategories

    /// <summary>
    /// Test ID: HOME-16
    /// Precondition: ItemService hoạt động bình thường, có categories trong hệ thống
    /// Input: Không có tham số đầu vào
    /// Condition: Lấy danh sách categories từ hệ thống
    /// Confirmation: HTTP 200 OK, trả về danh sách CategoryDto
    /// Type: Normal
    /// Test Requirement: Kiểm tra chức năng lấy categories thành công
    /// </summary>
    [Fact]
    public async Task GetCategories_WithValidData_ReturnsOk()
    {
        // Arrange
        var expectedCategories = new List<CategoryDto>
        {
            new() { Id = 1, Name = "Electronics" }
        };

        _itemServiceMock.Setup(x => x.GetCategoriesAsync())
            .ReturnsAsync(expectedCategories);

        // Act
        var result = await _controller.GetCategories();

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(expectedCategories);
    }

    /// <summary>
    /// Test ID: HOME-17
    /// Precondition: Service gặp lỗi hệ thống
    /// Input: Không có tham số đầu vào
    /// Condition: Service throw Exception (database error)
    /// Confirmation: HTTP 500 InternalServerError, thông báo Internal server error
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra xử lý exception từ service
    /// </summary>
    [Fact]
    public async Task GetCategories_WithException_ReturnsInternalServerError()
    {
        // Arrange
        _itemServiceMock.Setup(x => x.GetCategoriesAsync())
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetCategories();

        // Assert
        result.Result.Should().BeOfType<ObjectResult>();
        var objectResult = result.Result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
    }

    #endregion
}

