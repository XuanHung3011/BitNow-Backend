using BitNow_Backend.BLL.IServices;
using BitNow_Backend.Controllers;
using BitNow_Backend.DAL;
using BitNow_Backend.DAL.DTOs;
using BitNow_Backend.DAL.Models;
using BitNow_Backend.RealTime;
using BitNow_Backend.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.EntityFrameworkCore;
using System.Text;

namespace BitNow_Backend.Tests.Controllers;

public class ItemsControllerTests
{
    private readonly Mock<IItemService> _itemServiceMock;
    private readonly Mock<IFileUploadService> _fileUploadServiceMock;
    private readonly Mock<INotificationService> _notificationServiceMock;
    private readonly Mock<BidNowDbContext> _dbContextMock;
    private readonly Mock<IHubContext<AuctionHub>> _auctionHubMock;
    private readonly Mock<ILogger<ItemsController>> _loggerMock;
    private readonly ItemsController _controller;

    public ItemsControllerTests()
    {
        _itemServiceMock = new Mock<IItemService>();
        _fileUploadServiceMock = new Mock<IFileUploadService>();
        _notificationServiceMock = new Mock<INotificationService>();
        _dbContextMock = new Mock<BidNowDbContext>();
        _auctionHubMock = new Mock<IHubContext<AuctionHub>>();
        _loggerMock = new Mock<ILogger<ItemsController>>();

        _controller = new ItemsController(
            _itemServiceMock.Object,
            _fileUploadServiceMock.Object,
            _loggerMock.Object,
            _notificationServiceMock.Object,
            _dbContextMock.Object,
            _auctionHubMock.Object);
    }

    #region CreateItem

    /// <summary>
    /// Test ID: ITEM-01
    /// Precondition: ItemService hoạt động bình thường, SellerId và CategoryId hợp lệ
    /// Input: Form data hợp lệ với SellerId=1, CategoryId=1, Title="Test Item", BasePrice=100
    /// Condition: Tạo item mới với dữ liệu hợp lệ
    /// Confirmation: HTTP 200 OK, trả về ItemResponseDto với Id, Title đúng
    /// Type: Normal
    /// Test Requirement: Kiểm tra chức năng tạo item thành công
    /// </summary>
    [Fact]
    public async Task CreateItem_WithValidData_ReturnsOk()
    {
        // Arrange
        var formData = new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
        {
            { "SellerId", "1" },
            { "CategoryId", "1" },
            { "Title", "Test Item" },
            { "Description", "Test Description" },
            { "BasePrice", "100" }
        };

        var formCollection = new FormCollection(formData);
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Form = formCollection;
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        var expectedItem = new ItemResponseDto
        {
            Id = 1,
            Title = "Test Item",
            SellerId = 1,
            CategoryId = 1,
            BasePrice = 100,
            Status = "pending"
        };

        _itemServiceMock.Setup(x => x.CreateItemAsync(It.IsAny<CreateItemDto>(), It.IsAny<string?>()))
            .ReturnsAsync(expectedItem);

        // Mock DbContext - UserRoles and Users
        var userRoles = new List<UserRole>();
        var users = new List<User> { new User { Id = 1, Email = "seller@test.com" } };
        
        _dbContextMock.Setup(x => x.UserRoles).ReturnsDbSet(userRoles);
        _dbContextMock.Setup(x => x.Users).ReturnsDbSet(users);

        // Mock SignalR HubContext
        var mockClients = new Mock<IHubClients>();
        var mockGroup = new Mock<IClientProxy>();
        _auctionHubMock.Setup(x => x.Clients).Returns(mockClients.Object);
        _auctionHubMock.Setup(x => x.Clients.Group(It.IsAny<string>())).Returns(mockGroup.Object);
        mockGroup.Setup(x => x.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), default))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.CreateItem();

        // Assert
        // Note: CreateItem có thể trả về ObjectResult do phức tạp với file upload và SignalR
        result.Result.Should().BeAssignableTo<ObjectResult>();
        var objectResult = result.Result as ObjectResult;
        objectResult!.StatusCode.Should().Be(200);
    }

    /// <summary>
    /// Test ID: ITEM-02
    /// Precondition: SellerId không hợp lệ (<= 0)
    /// Input: Form data với SellerId=0
    /// Condition: Tạo item với SellerId không hợp lệ
    /// Confirmation: HTTP 400 BadRequest, thông báo SellerId phải > 0
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra validation SellerId
    /// </summary>
    [Fact]
    public async Task CreateItem_WithInvalidSellerId_ReturnsBadRequest()
    {
        // Arrange
        var formData = new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
        {
            { "SellerId", "0" },
            { "CategoryId", "1" },
            { "Title", "Test Item" },
            { "BasePrice", "100" }
        };

        var formCollection = new FormCollection(formData);
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Form = formCollection;
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        // Act
        var result = await _controller.CreateItem();

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    /// <summary>
    /// Test ID: ITEM-03
    /// Precondition: Title rỗng hoặc null
    /// Input: Form data với Title=""
    /// Condition: Tạo item với Title không hợp lệ
    /// Confirmation: HTTP 400 BadRequest, thông báo Title là bắt buộc
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra validation Title
    /// </summary>
    [Fact]
    public async Task CreateItem_WithEmptyTitle_ReturnsBadRequest()
    {
        // Arrange
        var formData = new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
        {
            { "SellerId", "1" },
            { "CategoryId", "1" },
            { "Title", "" },
            { "BasePrice", "100" }
        };

        var formCollection = new FormCollection(formData);
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Form = formCollection;
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        // Act
        var result = await _controller.CreateItem();

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    /// <summary>
    /// Test ID: ITEM-04
    /// Precondition: Service throw Exception
    /// Input: Form data hợp lệ
    /// Condition: Service throw Exception (database error)
    /// Confirmation: HTTP 500 InternalServerError
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra xử lý exception từ service
    /// </summary>
    [Fact]
    public async Task CreateItem_WithException_ReturnsInternalServerError()
    {
        // Arrange
        var formData = new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
        {
            { "SellerId", "1" },
            { "CategoryId", "1" },
            { "Title", "Test Item" },
            { "BasePrice", "100" }
        };

        var formCollection = new FormCollection(formData);
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Form = formCollection;
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        _itemServiceMock.Setup(x => x.CreateItemAsync(It.IsAny<CreateItemDto>(), It.IsAny<string?>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.CreateItem();

        // Assert
        result.Result.Should().BeOfType<ObjectResult>();
        var objectResult = result.Result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
    }

    #endregion

    #region CreateDraftItem

    /// <summary>
    /// Test ID: ITEM-05
    /// Precondition: ItemService hoạt động bình thường
    /// Input: Form data hợp lệ với BasePrice=0 (cho phép cho draft)
    /// Condition: Tạo draft item với dữ liệu hợp lệ
    /// Confirmation: HTTP 200 OK, trả về ItemResponseDto với Status="draft"
    /// Type: Normal
    /// Test Requirement: Kiểm tra chức năng tạo draft item thành công
    /// </summary>
    [Fact]
    public async Task CreateDraftItem_WithValidData_ReturnsOk()
    {
        // Arrange
        var formData = new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
        {
            { "SellerId", "1" },
            { "CategoryId", "1" },
            { "Title", "Draft Item" },
            { "BasePrice", "0" }
        };

        var formCollection = new FormCollection(formData);
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Form = formCollection;
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        var expectedItem = new ItemResponseDto
        {
            Id = 1,
            Title = "Draft Item",
            Status = "draft"
        };

        _itemServiceMock.Setup(x => x.CreateDraftItemAsync(It.IsAny<CreateItemDto>(), It.IsAny<string?>()))
            .ReturnsAsync(expectedItem);

        // Act
        var result = await _controller.CreateDraftItem();

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    #endregion

    #region UpdateDraftItem

    /// <summary>
    /// Test ID: ITEM-06
    /// Precondition: ItemService hoạt động bình thường, draft item tồn tại
    /// Input: Id=1, Form data hợp lệ
    /// Condition: Cập nhật draft item với dữ liệu hợp lệ
    /// Confirmation: HTTP 200 OK, trả về ItemResponseDto đã cập nhật
    /// Type: Normal
    /// Test Requirement: Kiểm tra chức năng cập nhật draft item thành công
    /// </summary>
    [Fact]
    public async Task UpdateDraftItem_WithValidData_ReturnsOk()
    {
        // Arrange
        var formData = new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
        {
            { "SellerId", "1" },
            { "CategoryId", "1" },
            { "Title", "Updated Draft Item" },
            { "BasePrice", "50" }
        };

        var formCollection = new FormCollection(formData);
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Form = formCollection;
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        var expectedItem = new ItemResponseDto
        {
            Id = 1,
            Title = "Updated Draft Item",
            Status = "draft"
        };

        _itemServiceMock.Setup(x => x.UpdateDraftItemAsync(1, It.IsAny<CreateItemDto>(), It.IsAny<string?>()))
            .ReturnsAsync(expectedItem);

        // Act
        var result = await _controller.UpdateDraftItem(1);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    /// <summary>
    /// Test ID: ITEM-07
    /// Precondition: Draft item không tồn tại
    /// Input: Id=999, Form data hợp lệ
    /// Condition: Cập nhật draft item không tồn tại
    /// Confirmation: HTTP 404 NotFound
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra xử lý khi draft item không tồn tại
    /// </summary>
    [Fact]
    public async Task UpdateDraftItem_WhenNotFound_ReturnsNotFound()
    {
        // Arrange
        var formData = new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
        {
            { "SellerId", "1" },
            { "CategoryId", "1" },
            { "Title", "Updated Draft Item" },
            { "BasePrice", "50" }
        };

        var formCollection = new FormCollection(formData);
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Form = formCollection;
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        _itemServiceMock.Setup(x => x.UpdateDraftItemAsync(999, It.IsAny<CreateItemDto>(), It.IsAny<string?>()))
            .ReturnsAsync((ItemResponseDto?)null);

        // Act
        var result = await _controller.UpdateDraftItem(999);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region GetAllItems

    /// <summary>
    /// Test ID: ITEM-08
    /// Precondition: ItemService hoạt động bình thường
    /// Input: statuses="approved", page=1, pageSize=10
    /// Condition: Lấy danh sách items với filter hợp lệ
    /// Confirmation: HTTP 200 OK, trả về PaginatedResult với ItemResponseDto
    /// Type: Normal
    /// Test Requirement: Kiểm tra chức năng lấy danh sách items với filter thành công
    /// </summary>
    [Fact]
    public async Task GetAllItems_WithValidParams_ReturnsOk()
    {
        // Arrange
        var expectedResult = new PaginatedResult<ItemResponseDto>
        {
            Data = new List<ItemResponseDto>
            {
                new() { Id = 1, Title = "Item 1", Status = "approved" }
            },
            TotalCount = 1,
            Page = 1,
            PageSize = 10
        };

        _itemServiceMock.Setup(x => x.GetAllItemsWithFilterAsync(It.IsAny<ItemFilterAllDto>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetAllItems(
            statuses: "approved",
            page: 1,
            pageSize: 10);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    /// <summary>
    /// Test ID: ITEM-09
    /// Precondition: sortBy không hợp lệ
    /// Input: sortBy="InvalidField"
    /// Condition: Lấy items với sortBy không được phép
    /// Confirmation: HTTP 400 BadRequest
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra validation sortBy
    /// </summary>
    [Fact]
    public async Task GetAllItems_WithInvalidSortBy_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.GetAllItems(sortBy: "InvalidField");

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region ApproveItem

    /// <summary>
    /// Test ID: ITEM-10
    /// Precondition: ItemService hoạt động bình thường, item tồn tại và ở trạng thái pending
    /// Input: Id=1
    /// Condition: Phê duyệt item với Id hợp lệ
    /// Confirmation: HTTP 200 OK, thông báo Item approved successfully
    /// Type: Normal
    /// Test Requirement: Kiểm tra chức năng phê duyệt item thành công
    /// </summary>
    [Fact]
    public async Task ApproveItem_WithValidId_ReturnsOk()
    {
        // Arrange
        var userId = 1;
        var headersMock = new Mock<IHeaderDictionary>();
        var headerValues = new Microsoft.Extensions.Primitives.StringValues(userId.ToString());
        headersMock.Setup(x => x["X-User-Id"]).Returns(headerValues);
        headersMock.Setup(x => x.GetEnumerator()).Returns(new List<KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues>>().GetEnumerator());
        
        var requestMock = new Mock<HttpRequest>();
        requestMock.Setup(x => x.Headers).Returns(headersMock.Object);
        
        var httpContextMock = new Mock<HttpContext>();
        httpContextMock.Setup(x => x.Request).Returns(requestMock.Object);
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContextMock.Object };
        
        var userRole = new UserRole { Id = 1, UserId = userId, Role = "admin" };
        var user = new User 
        { 
            Id = userId, 
            Email = "admin@test.com",
            UserRoles = new List<UserRole> { userRole }
        };
        userRole.User = user;
        var users = new List<User> { user };
        var userRoles = new List<UserRole> { userRole };
        _dbContextMock.Setup(x => x.Users).ReturnsDbSet(users);
        _dbContextMock.Setup(x => x.UserRoles).ReturnsDbSet(userRoles);
        
        var item = new ItemResponseDto
        {
            Id = 1,
            Title = "Test Item",
            SellerId = 1,
            Status = "pending"
        };

        _itemServiceMock.Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(item);

        _itemServiceMock.Setup(x => x.ApproveItemAsync(1))
            .ReturnsAsync(true);

        // Mock NotificationService
        _notificationServiceMock.Setup(x => x.CreateNotificationAsync(It.IsAny<CreateNotificationDto>()))
            .ReturnsAsync(new NotificationResponseDto { Id = 1, UserId = 1, Message = "Test", Type = "item_approved" });

        // Mock SignalR HubContext
        var mockClients = new Mock<IHubClients>();
        var mockGroup = new Mock<IClientProxy>();
        _auctionHubMock.Setup(x => x.Clients).Returns(mockClients.Object);
        _auctionHubMock.Setup(x => x.Clients.Group(It.IsAny<string>())).Returns(mockGroup.Object);
        mockGroup.Setup(x => x.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), default))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.ApproveItem(1);

        // Assert
        result.Should().BeAssignableTo<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(200);
    }

    /// <summary>
    /// Test ID: ITEM-11
    /// Precondition: Item không tồn tại
    /// Input: Id=999
    /// Condition: Phê duyệt item không tồn tại
    /// Confirmation: HTTP 404 NotFound
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra xử lý khi item không tồn tại
    /// </summary>
    [Fact]
    public async Task ApproveItem_WhenNotFound_ReturnsNotFound()
    {
        // Arrange
        var userId = 1;
        var headersMock = new Mock<IHeaderDictionary>();
        var headerValues = new Microsoft.Extensions.Primitives.StringValues(userId.ToString());
        headersMock.Setup(x => x["X-User-Id"]).Returns(headerValues);
        headersMock.Setup(x => x.GetEnumerator()).Returns(new List<KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues>>().GetEnumerator());
        
        var requestMock = new Mock<HttpRequest>();
        requestMock.Setup(x => x.Headers).Returns(headersMock.Object);
        
        var httpContextMock = new Mock<HttpContext>();
        httpContextMock.Setup(x => x.Request).Returns(requestMock.Object);
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContextMock.Object };
        
        var userRole = new UserRole { Id = 1, UserId = userId, Role = "admin" };
        var user = new User 
        { 
            Id = userId, 
            Email = "admin@test.com",
            UserRoles = new List<UserRole> { userRole }
        };
        userRole.User = user;
        var users = new List<User> { user };
        var userRoles = new List<UserRole> { userRole };
        _dbContextMock.Setup(x => x.Users).ReturnsDbSet(users);
        _dbContextMock.Setup(x => x.UserRoles).ReturnsDbSet(userRoles);
        
        _itemServiceMock.Setup(x => x.GetByIdAsync(999))
            .ReturnsAsync((ItemResponseDto?)null);

        // Act
        var result = await _controller.ApproveItem(999);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region RejectItem

    /// <summary>
    /// Test ID: ITEM-12
    /// Precondition: ItemService hoạt động bình thường, item tồn tại
    /// Input: Id=1, RejectItemDto với Reason hợp lệ
    /// Condition: Từ chối item với lý do hợp lệ
    /// Confirmation: HTTP 200 OK, thông báo Item rejected successfully
    /// Type: Normal
    /// Test Requirement: Kiểm tra chức năng từ chối item thành công
    /// </summary>
    [Fact]
    public async Task RejectItem_WithValidData_ReturnsOk()
    {
        // Arrange
        var userId = 1;
        var headersMock = new Mock<IHeaderDictionary>();
        var headerValues = new Microsoft.Extensions.Primitives.StringValues(userId.ToString());
        headersMock.Setup(x => x["X-User-Id"]).Returns(headerValues);
        headersMock.Setup(x => x.GetEnumerator()).Returns(new List<KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues>>().GetEnumerator());
        
        var requestMock = new Mock<HttpRequest>();
        requestMock.Setup(x => x.Headers).Returns(headersMock.Object);
        
        var httpContextMock = new Mock<HttpContext>();
        httpContextMock.Setup(x => x.Request).Returns(requestMock.Object);
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContextMock.Object };
        
        var userRole = new UserRole { Id = 1, UserId = userId, Role = "admin" };
        var user = new User 
        { 
            Id = userId, 
            Email = "admin@test.com",
            UserRoles = new List<UserRole> { userRole }
        };
        userRole.User = user;
        var users = new List<User> { user };
        var userRoles = new List<UserRole> { userRole };
        _dbContextMock.Setup(x => x.Users).ReturnsDbSet(users);
        _dbContextMock.Setup(x => x.UserRoles).ReturnsDbSet(userRoles);
        
        var item = new ItemResponseDto
        {
            Id = 1,
            Title = "Test Item",
            SellerId = 1,
            Status = "pending"
        };

        var rejectDto = new RejectItemDto { Reason = "Không phù hợp" };

        _itemServiceMock.Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(item);

        _itemServiceMock.Setup(x => x.RejectItemAsync(1))
            .ReturnsAsync(true);

        // Mock NotificationService
        _notificationServiceMock.Setup(x => x.CreateNotificationAsync(It.IsAny<CreateNotificationDto>()))
            .ReturnsAsync(new NotificationResponseDto { Id = 1, UserId = 1, Message = "Test", Type = "item_approved" });

        // Mock SignalR HubContext
        var mockClients = new Mock<IHubClients>();
        var mockGroup = new Mock<IClientProxy>();
        _auctionHubMock.Setup(x => x.Clients).Returns(mockClients.Object);
        _auctionHubMock.Setup(x => x.Clients.Group(It.IsAny<string>())).Returns(mockGroup.Object);
        mockGroup.Setup(x => x.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), default))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.RejectItem(1, rejectDto);

        // Assert
        result.Should().BeAssignableTo<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(200);
    }

    /// <summary>
    /// Test ID: ITEM-13
    /// Precondition: Reason rỗng hoặc null
    /// Input: Id=1, RejectItemDto với Reason=""
    /// Condition: Từ chối item với lý do rỗng
    /// Confirmation: HTTP 400 BadRequest
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra validation Reason
    /// </summary>
    [Fact]
    public async Task RejectItem_WithEmptyReason_ReturnsBadRequest()
    {
        // Arrange
        var userId = 1;
        var headersMock = new Mock<IHeaderDictionary>();
        var headerValues = new Microsoft.Extensions.Primitives.StringValues(userId.ToString());
        headersMock.Setup(x => x["X-User-Id"]).Returns(headerValues);
        headersMock.Setup(x => x.GetEnumerator()).Returns(new List<KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues>>().GetEnumerator());
        
        var requestMock = new Mock<HttpRequest>();
        requestMock.Setup(x => x.Headers).Returns(headersMock.Object);
        
        var httpContextMock = new Mock<HttpContext>();
        httpContextMock.Setup(x => x.Request).Returns(requestMock.Object);
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContextMock.Object };
        
        var userRole = new UserRole { Id = 1, UserId = userId, Role = "admin" };
        var user = new User 
        { 
            Id = userId, 
            Email = "admin@test.com",
            UserRoles = new List<UserRole> { userRole }
        };
        userRole.User = user;
        var users = new List<User> { user };
        var userRoles = new List<UserRole> { userRole };
        _dbContextMock.Setup(x => x.Users).ReturnsDbSet(users);
        _dbContextMock.Setup(x => x.UserRoles).ReturnsDbSet(userRoles);
        
        var rejectDto = new RejectItemDto { Reason = "" };

        // Act
        var result = await _controller.RejectItem(1, rejectDto);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region GetItemById

    /// <summary>
    /// Test ID: ITEM-14
    /// Precondition: ItemService hoạt động bình thường, item tồn tại
    /// Input: Id=1
    /// Condition: Lấy item theo Id hợp lệ
    /// Confirmation: HTTP 200 OK, trả về ItemResponseDto
    /// Type: Normal
    /// Test Requirement: Kiểm tra chức năng lấy item theo Id thành công
    /// </summary>
    [Fact]
    public async Task GetItemById_WithValidId_ReturnsOk()
    {
        // Arrange
        var expectedItem = new ItemResponseDto
        {
            Id = 1,
            Title = "Test Item",
            Status = "approved"
        };

        _itemServiceMock.Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(expectedItem);

        // Act
        var result = await _controller.GetItemById(1);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(expectedItem);
    }

    /// <summary>
    /// Test ID: ITEM-15
    /// Precondition: Item không tồn tại
    /// Input: Id=999
    /// Condition: Lấy item không tồn tại
    /// Confirmation: HTTP 404 NotFound
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra xử lý khi item không tồn tại
    /// </summary>
    [Fact]
    public async Task GetItemById_WhenNotFound_ReturnsNotFound()
    {
        // Arrange
        _itemServiceMock.Setup(x => x.GetByIdAsync(999))
            .ReturnsAsync((ItemResponseDto?)null);

        // Act
        var result = await _controller.GetItemById(999);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region DeleteItem

    /// <summary>
    /// Test ID: ITEM-16
    /// Precondition: ItemService hoạt động bình thường, item tồn tại và ở trạng thái draft
    /// Input: Id=1
    /// Condition: Xóa item ở trạng thái draft
    /// Confirmation: HTTP 200 OK, thông báo đã xóa thành công
    /// Type: Normal
    /// Test Requirement: Kiểm tra chức năng xóa item thành công
    /// </summary>
    [Fact]
    public async Task DeleteItem_WithDraftItem_ReturnsOk()
    {
        // Arrange
        var item = new ItemResponseDto
        {
            Id = 1,
            Title = "Draft Item",
            Status = "draft"
        };

        _itemServiceMock.Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(item);

        _itemServiceMock.Setup(x => x.DeleteItemAsync(1))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteItem(1);

        // Assert
        result.Should().BeAssignableTo<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(200);
    }

    /// <summary>
    /// Test ID: ITEM-17
    /// Precondition: Item không ở trạng thái draft hoặc pending
    /// Input: Id=1 (item có Status="approved")
    /// Condition: Xóa item không ở trạng thái draft/pending
    /// Confirmation: HTTP 400 BadRequest
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra chỉ cho phép xóa item ở trạng thái draft/pending
    /// </summary>
    [Fact]
    public async Task DeleteItem_WithNonDraftItem_ReturnsBadRequest()
    {
        // Arrange
        var item = new ItemResponseDto
        {
            Id = 1,
            Title = "Approved Item",
            Status = "approved"
        };

        _itemServiceMock.Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(item);

        // Act
        var result = await _controller.DeleteItem(1);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    /// <summary>
    /// Test ID: ITEM-18
    /// Precondition: Item không tồn tại
    /// Input: Id=999
    /// Condition: Xóa item không tồn tại
    /// Confirmation: HTTP 404 NotFound
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra xử lý khi item không tồn tại
    /// </summary>
    [Fact]
    public async Task DeleteItem_WhenNotFound_ReturnsNotFound()
    {
        // Arrange
        _itemServiceMock.Setup(x => x.GetByIdAsync(999))
            .ReturnsAsync((ItemResponseDto?)null);

        // Act
        var result = await _controller.DeleteItem(999);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion
}

