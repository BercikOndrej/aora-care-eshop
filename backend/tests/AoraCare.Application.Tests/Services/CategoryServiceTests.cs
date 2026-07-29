using AoraCare.Application.Dtos;
using AoraCare.Domain;
using AoraCare.Domain.Common;
using AoraCare.Domain.Models;
using AoraCare.Domain.Repositories.Interfaces;
using ErrorOr;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace AoraCare.Application.Services.Tests;

[Trait("Category", "Unit")]
public class CategoryServiceTests
{
    private readonly Mock<ICategoryRepository> _repoMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<ILogger<CategoryService>> _loggerMock = new();
    private readonly CategoryService _sut;

    public CategoryServiceTests()
    {
        _sut = new CategoryService(_repoMock.Object, _unitOfWorkMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetAllAsync_WhenCategoriesExist_ReturnsAllCategories()
    {
        var id = Guid.NewGuid();
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        List<CategoryResponseDto> expectedResult =
        [
            new CategoryResponseDto(id, "Name", "name", "description", 0, true),
            new CategoryResponseDto(id1, "Name1", "name1", "description1", 1, true),
            new CategoryResponseDto(id2, "Name2", "name2", "description2", 2, true),
        ];

        List<Category> dbCategories =
        [
            new Category
            {
                Id = id,
                Products = [],
                Name = "Name",
                Slug = "name",
                Description = "description",
                SortOrder = 0,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            },
            new Category
            {
                Id = id1,
                Products = [],
                Name = "Name1",
                Slug = "name1",
                Description = "description1",
                SortOrder = 1,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            },
            new Category
            {
                Id = id2,
                Products = [],
                Name = "Name2",
                Slug = "name2",
                Description = "description2",
                SortOrder = 2,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            },
        ];
        _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(dbCategories);

        var result = await _sut.GetAllAsync();

        result.Should().HaveCount(3);
        result.Should().BeEquivalentTo(expectedResult);
    }

    [Fact]
    public async Task GetAllAsync_WhenNoCategoriesExist_ReturnsEmptyList()
    {
        _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);

        var result = await _sut.GetAllAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_WhenRepositoryThrows_PropagatesException()
    {
        _repoMock
            .Setup(r => r.GetAllAsync())
            .ThrowsAsync(new InvalidOperationException("DB unavailable"));

        Func<Task> act = () => _sut.GetAllAsync();

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task GetAllActiveAsync_WhenCategoriesExist_ReturnsOnlyActive()
    {
        var id = Guid.NewGuid();
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        List<CategoryResponseDto> expectedResult =
        [
            new CategoryResponseDto(id, "Name", "name", "description", 0, true),
        ];

        List<Category> dbCategories =
        [
            new Category
            {
                Id = id,
                Products = [],
                Name = "Name",
                Slug = "name",
                Description = "description",
                SortOrder = 0,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            },
            new Category
            {
                Id = id1,
                Products = [],
                Name = "Name1",
                Slug = "name1",
                Description = "description1",
                SortOrder = 1,
                IsActive = false,
                CreatedAt = DateTime.UtcNow,
            },
            new Category
            {
                Id = id2,
                Products = [],
                Name = "Name2",
                Slug = "name2",
                Description = "description2",
                SortOrder = 2,
                IsActive = false,
                CreatedAt = DateTime.UtcNow,
            },
        ];
        _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(dbCategories);

        var result = await _sut.GetAllActiveAsync();

        result.Should().HaveCount(1);
        result.Should().BeEquivalentTo(expectedResult);
    }

    [Fact]
    public async Task GetAllActiveAsync_WhenOnlyInactiveCategoriesExist_ReturnsEmptyList()
    {
        var id = Guid.NewGuid();
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();

        List<Category> dbCategories =
        [
            new Category
            {
                Id = id,
                Products = [],
                Name = "Name",
                Slug = "name",
                Description = "description",
                SortOrder = 0,
                IsActive = false,
                CreatedAt = DateTime.UtcNow,
            },
            new Category
            {
                Id = id1,
                Products = [],
                Name = "Name1",
                Slug = "name1",
                Description = "description1",
                SortOrder = 1,
                IsActive = false,
                CreatedAt = DateTime.UtcNow,
            },
            new Category
            {
                Id = id2,
                Products = [],
                Name = "Name2",
                Slug = "name2",
                Description = "description2",
                SortOrder = 2,
                IsActive = false,
                CreatedAt = DateTime.UtcNow,
            },
        ];
        _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(dbCategories);

        var result = await _sut.GetAllActiveAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task AddAsync_WhenDataIsCorrect_ReturnsCreatedResponseDto()
    {
        string name = "Test";
        string description = "Test description";

        CategoryAddDto addDto = new(name, description, false);

        Category? captured = null;
        _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _repoMock.Setup(r => r.Add(It.IsAny<Category>())).Callback<Category>(c => captured = c);

        var result = await _sut.AddAsync(addDto);

        result.IsError.Should().BeFalse();
        _repoMock.Verify(r => r.Add(It.IsAny<Category>()), Times.Once);
        _repoMock.Verify(r => r.GetAllAsync(), Times.AtLeastOnce);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);

        captured.Should().NotBeNull();
        CategoryResponseDto expected = new(captured!.Id, name, "test", description, 0, false);
        result.Value.Should().Be(expected);
    }

    [Fact]
    public async Task AddAsync_WhenCategoriesExist_SortOrderIsMaxPlusOne()
    {
        List<Category> existing =
        [
            new Category
            {
                Id = Guid.NewGuid(),
                Products = [],
                Name = "A",
                Slug = "a",
                Description = "description",
                SortOrder = 0,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            },
            new Category
            {
                Id = Guid.NewGuid(),
                Products = [],
                Name = "B",
                Slug = "b",
                Description = "description",
                SortOrder = 3,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            },
        ];
        CategoryAddDto addDto = new("Test", "Test description", true);

        Category? captured = null;
        _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(existing);
        _repoMock.Setup(r => r.Add(It.IsAny<Category>())).Callback<Category>(c => captured = c);

        var result = await _sut.AddAsync(addDto);

        result.IsError.Should().BeFalse();
        captured.Should().NotBeNull();
        captured!.SortOrder.Should().Be(4);
    }

    [Fact]
    public async Task AddAsync_WhenIsActiveNotProvided_DefaultsToTrue()
    {
        CategoryAddDto addDto = new("Test", "Test description", null);

        Category? captured = null;
        _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _repoMock.Setup(r => r.Add(It.IsAny<Category>())).Callback<Category>(c => captured = c);

        var result = await _sut.AddAsync(addDto);

        result.IsError.Should().BeFalse();
        captured.Should().NotBeNull();
        captured!.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task AddAsync_WhenSlugAlreadyExists_ReturnsConflictAndDoesNotPersist()
    {
        List<Category> existing =
        [
            new Category
            {
                Id = Guid.NewGuid(),
                Products = [],
                Name = "Test",
                Slug = "test",
                Description = "description",
                SortOrder = 0,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            },
        ];
        CategoryAddDto addDto = new("Test", "Test description", true);

        _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(existing);

        var result = await _sut.AddAsync(addDto);

        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Conflict);
        _repoMock.Verify(r => r.Add(It.IsAny<Category>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task GetAsync_WhenCategoryHasNoProducts_ReturnsMappedDtoWithEmptyProducts()
    {
        var id = Guid.NewGuid();
        var category = new Category
        {
            Id = id,
            Products = [],
            Name = "Name",
            Slug = "name",
            Description = "description",
            SortOrder = 0,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };
        _repoMock.Setup(r => r.GetCategoryWithProductsByIdAsync(id)).ReturnsAsync(category);

        var result = await _sut.GetByIdAsync(id);

        result.IsError.Should().BeFalse();
        result.Value.Should().BeEquivalentTo(category.ToDetailDto());
        result.Value.Products.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAsync_WhenCategoryHasProducts_ReturnsMappedDtoWithProducts()
    {
        var id = Guid.NewGuid();
        var product1 = new Product
        {
            Id = Guid.NewGuid(),
            CategoryId = id,
            Name = "Product 1",
            Slug = "product-1",
            Description = "description",
            ImageUrl = "https://example.com/1.png",
            SortOrder = 0,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        var product2 = new Product
        {
            Id = Guid.NewGuid(),
            CategoryId = id,
            Name = "Product 2",
            Slug = "product-2",
            Description = "description",
            ImageUrl = "https://example.com/2.png",
            SortOrder = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        var category = new Category
        {
            Id = id,
            Products = [product1, product2],
            Name = "Name",
            Slug = "name",
            Description = "description",
            SortOrder = 0,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };
        _repoMock.Setup(r => r.GetCategoryWithProductsByIdAsync(id)).ReturnsAsync(category);

        var result = await _sut.GetByIdAsync(id);

        result.IsError.Should().BeFalse();
        result.Value.Products.Should().BeEquivalentTo([product1.ToDto(), product2.ToDto()]);
    }

    [Fact]
    public async Task GetAsync_WhenCategoryNotFound_ReturnsNotFoundError()
    {
        var id = Guid.NewGuid();
        _repoMock.Setup(r => r.GetCategoryWithProductsByIdAsync(id)).ReturnsAsync((Category?)null);

        var result = await _sut.GetByIdAsync(id);

        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task UpdateAsync_WhenCategoryNotFound_ReturnsNotFoundError()
    {
        var id = Guid.NewGuid();
        _repoMock.Setup(r => r.GetByIdForUpdateAsync(id)).ReturnsAsync((Category?)null);
        CategoryUpdateDto updateDto = new(null, null, null, null);

        var result = await _sut.UpdateAsync(id, updateDto);

        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.NotFound);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenNameChangedToUniqueValue_UpdatesNameAndSlugAndSaves()
    {
        var id = Guid.NewGuid();
        var category = new Category
        {
            Id = id,
            Products = [],
            Name = "Old Name",
            Slug = "old-name",
            Description = "description",
            SortOrder = 0,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };
        _repoMock.Setup(r => r.GetByIdForUpdateAsync(id)).ReturnsAsync(category);
        _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([category]);
        CategoryUpdateDto updateDto = new("New Name", null, null, null);

        var result = await _sut.UpdateAsync(id, updateDto);

        result.IsError.Should().BeFalse();
        category.Name.Should().Be("New Name");
        category.Slug.Should().Be("new-name");
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenNameChangedToConflictingSlug_ReturnsConflictAndDoesNotSave()
    {
        var id = Guid.NewGuid();
        var category = new Category
        {
            Id = id,
            Products = [],
            Name = "Old Name",
            Slug = "old-name",
            Description = "description",
            SortOrder = 0,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };
        var other = new Category
        {
            Id = Guid.NewGuid(),
            Products = [],
            Name = "New Name",
            Slug = "new-name",
            Description = "description",
            SortOrder = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };
        _repoMock.Setup(r => r.GetByIdForUpdateAsync(id)).ReturnsAsync(category);
        _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([category, other]);
        CategoryUpdateDto updateDto = new("New Name", null, null, null);

        var result = await _sut.UpdateAsync(id, updateDto);

        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Conflict);
        category.Name.Should().Be("Old Name");
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenSortOrderWithinRange_ReordersCategoriesAndSaves()
    {
        var id = Guid.NewGuid();
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var category = new Category
        {
            Id = id,
            Products = [],
            Name = "A",
            Slug = "a",
            Description = "description",
            SortOrder = 0,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };
        var category1 = new Category
        {
            Id = id1,
            Products = [],
            Name = "B",
            Slug = "b",
            Description = "description",
            SortOrder = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };
        var category2 = new Category
        {
            Id = id2,
            Products = [],
            Name = "C",
            Slug = "c",
            Description = "description",
            SortOrder = 2,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };
        List<Category> all = [category, category1, category2];

        _repoMock.Setup(r => r.GetByIdForUpdateAsync(id)).ReturnsAsync(category);
        _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(all);
        _repoMock.Setup(r => r.GetAllForUpdateAsync()).ReturnsAsync(all);
        CategoryUpdateDto updateDto = new(null, null, 2, null);

        var result = await _sut.UpdateAsync(id, updateDto);

        result.IsError.Should().BeFalse();
        category.SortOrder.Should().Be(2);
        category1.SortOrder.Should().Be(0);
        category2.SortOrder.Should().Be(1);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenSortOrderOutOfRange_ReturnsValidationErrorAndDoesNotSave()
    {
        var id = Guid.NewGuid();
        var category = new Category
        {
            Id = id,
            Products = [],
            Name = "A",
            Slug = "a",
            Description = "description",
            SortOrder = 0,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };
        _repoMock.Setup(r => r.GetByIdForUpdateAsync(id)).ReturnsAsync(category);
        _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([category]);
        CategoryUpdateDto updateDto = new(null, null, 5, null);

        var result = await _sut.UpdateAsync(id, updateDto);

        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Validation);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenSortOrderIsZero_MovesCategoryToFirstPositionAndSaves()
    {
        var id = Guid.NewGuid();
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var category = new Category
        {
            Id = id,
            Products = [],
            Name = "A",
            Slug = "a",
            Description = "description",
            SortOrder = 2,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };
        var category1 = new Category
        {
            Id = id1,
            Products = [],
            Name = "B",
            Slug = "b",
            Description = "description",
            SortOrder = 0,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };
        var category2 = new Category
        {
            Id = id2,
            Products = [],
            Name = "C",
            Slug = "c",
            Description = "description",
            SortOrder = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };
        List<Category> all = [category1, category2, category];

        _repoMock.Setup(r => r.GetByIdForUpdateAsync(id)).ReturnsAsync(category);
        _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(all);
        _repoMock.Setup(r => r.GetAllForUpdateAsync()).ReturnsAsync(all);
        CategoryUpdateDto updateDto = new(null, null, 0, null);

        var result = await _sut.UpdateAsync(id, updateDto);

        result.IsError.Should().BeFalse();
        category.SortOrder.Should().Be(0);
        category1.SortOrder.Should().Be(1);
        category2.SortOrder.Should().Be(2);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenSortOrderEqualsCategoryCount_ReturnsValidationErrorAndDoesNotSave()
    {
        var id = Guid.NewGuid();
        var category = new Category
        {
            Id = id,
            Products = [],
            Name = "A",
            Slug = "a",
            Description = "description",
            SortOrder = 0,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };
        var other = new Category
        {
            Id = Guid.NewGuid(),
            Products = [],
            Name = "B",
            Slug = "b",
            Description = "description",
            SortOrder = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };
        _repoMock.Setup(r => r.GetByIdForUpdateAsync(id)).ReturnsAsync(category);
        _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([category, other]);
        CategoryUpdateDto updateDto = new(null, null, 2, null);

        var result = await _sut.UpdateAsync(id, updateDto);

        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Validation);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenOptionalFieldsAreNull_KeepsExistingValues()
    {
        var id = Guid.NewGuid();
        var category = new Category
        {
            Id = id,
            Products = [],
            Name = "Name",
            Slug = "name",
            Description = "description",
            SortOrder = 0,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };
        _repoMock.Setup(r => r.GetByIdForUpdateAsync(id)).ReturnsAsync(category);
        CategoryUpdateDto updateDto = new(null, null, null, null);

        var result = await _sut.UpdateAsync(id, updateDto);

        result.IsError.Should().BeFalse();
        category.Name.Should().Be("Name");
        category.Slug.Should().Be("name");
        category.Description.Should().Be("description");
        category.IsActive.Should().BeTrue();
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenCategoryNotFound_ReturnsNotFoundError()
    {
        var id = Guid.NewGuid();
        _repoMock.Setup(r => r.GetAllForUpdateAsync()).ReturnsAsync([]);

        var result = await _sut.DeleteAsync(id);

        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.NotFound);
        _repoMock.Verify(r => r.Remove(It.IsAny<Category>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WhenCategoryExists_RemovesAndReindexesRemainingCategoriesAndSaves()
    {
        var id = Guid.NewGuid();
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var category = new Category
        {
            Id = id,
            Products = [],
            Name = "A",
            Slug = "a",
            Description = "description",
            SortOrder = 0,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };
        var category1 = new Category
        {
            Id = id1,
            Products = [],
            Name = "B",
            Slug = "b",
            Description = "description",
            SortOrder = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };
        var category2 = new Category
        {
            Id = id2,
            Products = [],
            Name = "C",
            Slug = "c",
            Description = "description",
            SortOrder = 2,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };
        _repoMock
            .Setup(r => r.GetAllForUpdateAsync())
            .ReturnsAsync([category, category1, category2]);

        var result = await _sut.DeleteAsync(id1);

        result.IsError.Should().BeFalse();
        _repoMock.Verify(r => r.Remove(category1), Times.Once);
        category.SortOrder.Should().Be(0);
        category2.SortOrder.Should().Be(1);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }
}
