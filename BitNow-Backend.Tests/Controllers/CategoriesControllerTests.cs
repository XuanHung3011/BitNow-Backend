using BitNow_Backend.BLL.IServices;
using BitNow_Backend.Controllers;
using BitNow_Backend.DAL;
using BitNow_Backend.DAL.DTOs;
using BitNow_Backend.DAL.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Moq;
using Moq.EntityFrameworkCore;

namespace BitNow_Backend.Tests.Controllers;

public class CategoriesControllerTests
{
    private readonly Mock<ICategoryService> _categoryServiceMock;
    private readonly Mock<BidNowDbContext> _dbContextMock;
    private readonly CategoriesController _controller;

    public CategoriesControllerTests()
    {
        _categoryServiceMock = new Mock<ICategoryService>();
        _dbContextMock = new Mock<BidNowDbContext>();
        _controller = new CategoriesController(_categoryServiceMock.Object, _dbContextMock.Object);
    }

    private void SetupHttpContext(int userId)
    {
        // Use DefaultHttpContext which properly initializes Request
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-User-Id"] = userId.ToString();
        
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    private void SetupDbContextForRoleCheck(int userId, string roleName)
    {
        var userRole = new UserRole { Id = 1, UserId = userId, Role = roleName };
        var user = new User 
        { 
            Id = userId, 
            Email = $"{roleName}@test.com",
            UserRoles = new List<UserRole> { userRole }
        };
        userRole.User = user;
        var users = new List<User> { user };
        var userRoles = new List<UserRole> { userRole };
        
        _dbContextMock.Setup(x => x.Users).ReturnsDbSet(users);
        _dbContextMock.Setup(x => x.UserRoles).ReturnsDbSet(userRoles);
    }

    /// <summary>
    /// Test ID: CAT-01
    /// Precondition: CategoryService hoạt động bình thường, có categories trong hệ thống
    /// Input: Không có tham số đầu vào
    /// Condition: Lấy tất cả categories từ hệ thống
    /// Confirmation: HTTP 200 OK, trả về danh sách CategoryDtos
    /// Type: Normal
    /// Test Requirement: Kiểm tra chức năng lấy tất cả categories thành công
    /// </summary>
    [Fact]
    public async Task GetAllCategories_ReturnsOk()
    {
        // Arrange
        var categories = new List<CategoryDtos>
        {
            new CategoryDtos { Id = 1, Name = "Electronics", Slug = "electronics" },
            new CategoryDtos { Id = 2, Name = "Clothing", Slug = "clothing" }
        };

        _categoryServiceMock.Setup(x => x.GetAllCategoriesAsync())
            .ReturnsAsync(categories);

        // Act
        var result = await _controller.GetAllCategories();

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(categories);
    }

    /// <summary>
    /// Test ID: CAT-02
    /// Precondition: Category tồn tại trong hệ thống, CategoryService hoạt động bình thường
    /// Input: CategoryId hợp lệ (1)
    /// Condition: Lấy category theo ID hợp lệ
    /// Confirmation: HTTP 200 OK, trả về CategoryDtos với Id=1, Name="Electronics"
    /// Type: Normal
    /// Test Requirement: Kiểm tra chức năng lấy category theo ID thành công
    /// </summary>
    [Fact]
    public async Task GetCategory_WithValidId_ReturnsOk()
    {
        // Arrange
        var category = new CategoryDtos
        {
            Id = 1,
            Name = "Electronics",
            Slug = "electronics"
        };

        _categoryServiceMock.Setup(x => x.GetCategoryByIdAsync(1))
            .ReturnsAsync(category);

        // Act
        var result = await _controller.GetCategory(1);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(category);
    }

    /// <summary>
    /// Test ID: CAT-03
    /// Precondition: Category không tồn tại trong hệ thống
    /// Input: CategoryId không tồn tại (999)
    /// Condition: Lấy category với ID không tồn tại
    /// Confirmation: HTTP 404 NotFound
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra xử lý trường hợp category không tồn tại
    /// </summary>
    [Fact]
    public async Task GetCategory_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        _categoryServiceMock.Setup(x => x.GetCategoryByIdAsync(999))
            .ReturnsAsync((CategoryDtos?)null);

        // Act
        var result = await _controller.GetCategory(999);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    /// <summary>
    /// Test ID: CAT-04
    /// Precondition: Category tồn tại trong hệ thống với slug hợp lệ, CategoryService hoạt động bình thường
    /// Input: Slug hợp lệ ("electronics")
    /// Condition: Lấy category theo slug hợp lệ
    /// Confirmation: HTTP 200 OK, trả về CategoryDtos với Slug="electronics"
    /// Type: Normal
    /// Test Requirement: Kiểm tra chức năng lấy category theo slug thành công
    /// </summary>
    [Fact]
    public async Task GetCategoryBySlug_WithValidSlug_ReturnsOk()
    {
        // Arrange
        var category = new CategoryDtos
        {
            Id = 1,
            Name = "Electronics",
            Slug = "electronics"
        };

        _categoryServiceMock.Setup(x => x.GetCategoryBySlugAsync("electronics"))
            .ReturnsAsync(category);

        // Act
        var result = await _controller.GetCategoryBySlug("electronics");

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(category);
    }

    /// <summary>
    /// Test ID: CAT-05
    /// Precondition: Slug chưa tồn tại trong hệ thống, CategoryService hoạt động bình thường
    /// Input: CreateCategoryDtos hợp lệ (Name="Electronics", Slug="electronics", Description="Electronic items")
    /// Condition: Tạo category mới với thông tin hợp lệ và slug chưa tồn tại
    /// Confirmation: HTTP 201 Created, trả về CategoryDtos với Id=1
    /// Type: Normal
    /// Test Requirement: Kiểm tra chức năng tạo category thành công
    /// </summary>
    [Fact]
    public async Task CreateCategory_WithValidData_ReturnsCreated()
    {
        // Arrange
        var userId = 1;
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-User-Id"] = userId.ToString();
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        SetupDbContextForRoleCheck(userId, "admin");
        
        var createDto = new CreateCategoryDtos
        {
            Name = "Electronics",
            Slug = "electronics",
            Description = "Electronic items"
        };

        var createdCategory = new CategoryDtos
        {
            Id = 1,
            Name = "Electronics",
            Slug = "electronics",
            Description = "Electronic items"
        };

        _categoryServiceMock.Setup(x => x.CreateCategoryAsync(createDto))
            .ReturnsAsync(createdCategory);

        // Act
        var result = await _controller.CreateCategory(createDto);

        // Assert
        result.Result.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = result.Result as CreatedAtActionResult;
        createdResult!.Value.Should().BeEquivalentTo(createdCategory);
    }

    /// <summary>
    /// Test ID: CAT-06
    /// Precondition: Slug đã tồn tại trong hệ thống
    /// Input: CreateCategoryDtos với Slug đã tồn tại ("electronics")
    /// Condition: Tạo category với slug trùng lặp
    /// Confirmation: HTTP 409 Conflict, thông báo "Slug already exists"
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra xử lý trường hợp slug trùng lặp
    /// </summary>
    [Fact]
    public async Task CreateCategory_WithDuplicateSlug_ReturnsConflict()
    {
        // Arrange
        var userId = 1;
        SetupHttpContext(userId);
        SetupDbContextForRoleCheck(userId, "admin");
        
        var createDto = new CreateCategoryDtos
        {
            Name = "Electronics",
            Slug = "electronics"
        };

        _categoryServiceMock.Setup(x => x.CreateCategoryAsync(createDto))
            .ThrowsAsync(new InvalidOperationException("Slug already exists"));

        // Act
        var result = await _controller.CreateCategory(createDto);

        // Assert
        result.Result.Should().BeOfType<ConflictObjectResult>();
    }

    /// <summary>
    /// Test ID: CAT-07
    /// Precondition: Category tồn tại trong hệ thống, CategoryService hoạt động bình thường
    /// Input: CategoryId hợp lệ (1), UpdateCategoryDtos hợp lệ
    /// Condition: Cập nhật category với thông tin hợp lệ
    /// Confirmation: HTTP 200 OK, trả về CategoryDtos đã được cập nhật
    /// Type: Normal
    /// Test Requirement: Kiểm tra chức năng cập nhật category thành công
    /// </summary>
    [Fact]
    public async Task UpdateCategory_WithValidData_ReturnsOk()
    {
        // Arrange
        var userId = 1;
        SetupHttpContext(userId);
        SetupDbContextForRoleCheck(userId, "admin");
        
        var updateDto = new UpdateCategoryDtos
        {
            Name = "Updated Electronics",
            Slug = "electronics",
            Description = "Updated description"
        };

        var updatedCategory = new CategoryDtos
        {
            Id = 1,
            Name = "Updated Electronics",
            Slug = "electronics",
            Description = "Updated description"
        };

        _categoryServiceMock.Setup(x => x.UpdateCategoryAsync(1, updateDto))
            .ReturnsAsync(updatedCategory);

        // Act
        var result = await _controller.UpdateCategory(1, updateDto);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(updatedCategory);
    }

    /// <summary>
    /// Test ID: CAT-08
    /// Precondition: Category không tồn tại trong hệ thống
    /// Input: CategoryId không tồn tại (999), UpdateCategoryDtos hợp lệ
    /// Condition: Cập nhật category với ID không tồn tại
    /// Confirmation: HTTP 404 NotFound
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra xử lý trường hợp category không tồn tại khi cập nhật
    /// </summary>
    [Fact]
    public async Task UpdateCategory_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var userId = 1;
        SetupHttpContext(userId);
        SetupDbContextForRoleCheck(userId, "admin");
        
        var updateDto = new UpdateCategoryDtos
        {
            Name = "Updated Electronics",
            Slug = "electronics"
        };

        _categoryServiceMock.Setup(x => x.UpdateCategoryAsync(999, updateDto))
            .ReturnsAsync((CategoryDtos?)null);

        // Act
        var result = await _controller.UpdateCategory(999, updateDto);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    /// <summary>
    /// Test ID: CAT-09
    /// Precondition: Category tồn tại và không được sử dụng bởi items/auctions khác
    /// Input: CategoryId hợp lệ (1)
    /// Condition: Xóa category không được sử dụng
    /// Confirmation: HTTP 204 NoContent
    /// Type: Normal
    /// Test Requirement: Kiểm tra chức năng xóa category thành công
    /// </summary>
    [Fact]
    public async Task DeleteCategory_WithValidId_ReturnsNoContent()
    {
        // Arrange
        var userId = 1;
        SetupHttpContext(userId);
        SetupDbContextForRoleCheck(userId, "admin");
        
        _categoryServiceMock.Setup(x => x.DeleteCategoryAsync(1))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteCategory(1);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    /// <summary>
    /// Test ID: CAT-10
    /// Precondition: Category không tồn tại trong hệ thống
    /// Input: CategoryId không tồn tại (999)
    /// Condition: Xóa category không tồn tại
    /// Confirmation: HTTP 404 NotFound
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra xử lý trường hợp category không tồn tại khi xóa
    /// </summary>
    [Fact]
    public async Task DeleteCategory_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var userId = 1;
        SetupHttpContext(userId);
        SetupDbContextForRoleCheck(userId, "admin");
        
        _categoryServiceMock.Setup(x => x.DeleteCategoryAsync(999))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.DeleteCategory(999);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    /// <summary>
    /// Test ID: CAT-11
    /// Precondition: Category đang được sử dụng bởi items/auctions
    /// Input: CategoryId hợp lệ (1) nhưng đang được sử dụng
    /// Condition: Xóa category đang được sử dụng
    /// Confirmation: HTTP 409 Conflict, thông báo "Category is in use"
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra xử lý trường hợp category đang được sử dụng
    /// </summary>
    [Fact]
    public async Task DeleteCategory_WithCategoryInUse_ReturnsConflict()
    {
        // Arrange
        var userId = 1;
        SetupHttpContext(userId);
        SetupDbContextForRoleCheck(userId, "admin");
        
        _categoryServiceMock.Setup(x => x.DeleteCategoryAsync(1))
            .ThrowsAsync(new InvalidOperationException("Category is in use"));

        // Act
        var result = await _controller.DeleteCategory(1);

        // Assert
        result.Should().BeOfType<ConflictObjectResult>();
    }

    /// <summary>
    /// Test ID: CAT-12
    /// Precondition: CategoryService hoạt động bình thường, có categories trong hệ thống
    /// Input: CategoryFilterDto với Page=1, PageSize=10 (hoặc không có tham số)
    /// Condition: Lấy danh sách categories có phân trang
    /// Confirmation: HTTP 200 OK, trả về PaginatedResult với Data, TotalCount, Page, PageSize
    /// Type: Normal
    /// Test Requirement: Kiểm tra chức năng lấy categories có phân trang thành công
    /// </summary>
    [Fact]
    public async Task GetCategoriesPaged_ReturnsOk()
    {
        // Arrange
        var filter = new CategoryFilterDto
        {
            Page = 1,
            PageSize = 10
        };

        var paginatedResult = new PaginatedResult<CategoryDtos>
        {
            Data = new List<CategoryDtos>
            {
                new CategoryDtos { Id = 1, Name = "Electronics", Slug = "electronics" }
            },
            TotalCount = 1,
            Page = 1,
            PageSize = 10
        };

        _categoryServiceMock.Setup(x => x.GetCategoriesPagedAsync(It.IsAny<CategoryFilterDto>()))
            .ReturnsAsync(paginatedResult);

        // Act
        var result = await _controller.GetCategoriesPaged();

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    /// <summary>
    /// Test ID: CAT-13
    /// Precondition: CategoryService hoạt động bình thường
    /// Input: Slug hợp lệ ("electronics")
    /// Condition: Kiểm tra slug đã tồn tại trong hệ thống
    /// Confirmation: HTTP 200 OK, trả về true nếu slug tồn tại
    /// Type: Normal
    /// Test Requirement: Kiểm tra chức năng kiểm tra slug tồn tại thành công
    /// </summary>
    [Fact]
    public async Task CheckSlugExists_ReturnsOk()
    {
        // Arrange
        _categoryServiceMock.Setup(x => x.SlugExistsAsync("electronics", null))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.CheckSlugExists("electronics");

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult!.Value.Should().Be(true);
    }

    /// <summary>
    /// Test ID: CAT-14
    /// Precondition: Category tồn tại trong hệ thống, CategoryService hoạt động bình thường
    /// Input: CategoryId hợp lệ (1)
    /// Condition: Kiểm tra category có đang được sử dụng không
    /// Confirmation: HTTP 200 OK, trả về true nếu category đang được sử dụng
    /// Type: Normal
    /// Test Requirement: Kiểm tra chức năng kiểm tra category đang được sử dụng thành công
    /// </summary>
    [Fact]
    public async Task IsCategoryInUse_WithValidId_ReturnsOk()
    {
        // Arrange
        _categoryServiceMock.Setup(x => x.IsCategoryInUseAsync(1))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.IsCategoryInUse(1);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }
}

