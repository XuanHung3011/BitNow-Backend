using BitNow_Backend.BLL.IServices;
using BitNow_Backend.Controllers;
using BitNow_Backend.DAL.DTOs;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace BitNow_Backend.Tests.Controllers;

public class CategoriesControllerTests
{
    private readonly Mock<ICategoryService> _categoryServiceMock;
    private readonly CategoriesController _controller;

    public CategoriesControllerTests()
    {
        _categoryServiceMock = new Mock<ICategoryService>();
        _controller = new CategoriesController(_categoryServiceMock.Object);
    }

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

    [Fact]
    public async Task CreateCategory_WithValidData_ReturnsCreated()
    {
        // Arrange
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

    [Fact]
    public async Task CreateCategory_WithDuplicateSlug_ReturnsConflict()
    {
        // Arrange
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

    [Fact]
    public async Task UpdateCategory_WithValidData_ReturnsOk()
    {
        // Arrange
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

    [Fact]
    public async Task UpdateCategory_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
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

    [Fact]
    public async Task DeleteCategory_WithValidId_ReturnsNoContent()
    {
        // Arrange
        _categoryServiceMock.Setup(x => x.DeleteCategoryAsync(1))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteCategory(1);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteCategory_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        _categoryServiceMock.Setup(x => x.DeleteCategoryAsync(999))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.DeleteCategory(999);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task DeleteCategory_WithCategoryInUse_ReturnsConflict()
    {
        // Arrange
        _categoryServiceMock.Setup(x => x.DeleteCategoryAsync(1))
            .ThrowsAsync(new InvalidOperationException("Category is in use"));

        // Act
        var result = await _controller.DeleteCategory(1);

        // Assert
        result.Should().BeOfType<ConflictObjectResult>();
    }

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

