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
public class ProductServiceTests
{
    private readonly Mock<IProductRepository> _productRepoMock = new();
    private readonly Mock<ICategoryRepository> _categoryRepoMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<ILogger<ProductService>> _loggerMock = new();
    private readonly ProductService _sut;

    public ProductServiceTests()
    {
        _sut = new ProductService(
            _loggerMock.Object,
            _unitOfWorkMock.Object,
            _productRepoMock.Object,
            _categoryRepoMock.Object
        );
    }

    private static Product CreateProduct(
        Guid id,
        Guid categoryId,
        string name = "A",
        string slug = "a",
        int sortOrder = 0,
        bool isActive = true
    ) =>
        new()
        {
            Id = id,
            CategoryId = categoryId,
            Name = name,
            Slug = slug,
            Description = "description",
            ImageUrl = null,
            SortOrder = sortOrder,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

    [Fact]
    public async Task GetAllAsync_WhenProductsExist_ReturnsAllProducts()
    {
        var categoryId = Guid.NewGuid();
        var product = CreateProduct(Guid.NewGuid(), categoryId);
        var product1 = CreateProduct(Guid.NewGuid(), categoryId, "B", "b", 1);
        List<Product> dbProducts = [product, product1];
        _productRepoMock.Setup(r => r.GetAllAsync(default)).ReturnsAsync(dbProducts);

        var result = await _sut.GetAllAsync();

        result.Should().HaveCount(2);
        result.Should().BeEquivalentTo([product.ToDto(), product1.ToDto()]);
    }

    [Fact]
    public async Task GetAllAsync_WhenNoProductsExist_ReturnsEmptyList()
    {
        _productRepoMock.Setup(r => r.GetAllAsync(default)).ReturnsAsync([]);

        var result = await _sut.GetAllAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_WhenRepositoryThrows_PropagatesException()
    {
        _productRepoMock
            .Setup(r => r.GetAllAsync(default))
            .ThrowsAsync(new InvalidOperationException("DB unavailable"));

        Func<Task> act = () => _sut.GetAllAsync();

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task GetAllActiveAsync_WhenProductsExist_ReturnsOnlyActive()
    {
        var categoryId = Guid.NewGuid();
        var active = CreateProduct(Guid.NewGuid(), categoryId, "A", "a", 0, true);
        var inactive = CreateProduct(Guid.NewGuid(), categoryId, "B", "b", 1, false);
        _productRepoMock.Setup(r => r.GetAllAsync(default)).ReturnsAsync([active, inactive]);

        var result = await _sut.GetAllActiveAsync();

        result.Should().HaveCount(1);
        result.Should().BeEquivalentTo([active.ToDto()]);
    }

    [Fact]
    public async Task GetAllActiveAsync_WhenOnlyInactiveProductsExist_ReturnsEmptyList()
    {
        var categoryId = Guid.NewGuid();
        var inactive = CreateProduct(Guid.NewGuid(), categoryId, "A", "a", 0, false);
        var inactive1 = CreateProduct(Guid.NewGuid(), categoryId, "B", "b", 1, false);
        _productRepoMock.Setup(r => r.GetAllAsync(default)).ReturnsAsync([inactive, inactive1]);

        var result = await _sut.GetAllActiveAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByIdAsync_WhenProductExists_ReturnsMappedDto()
    {
        var id = Guid.NewGuid();
        var product = CreateProduct(id, Guid.NewGuid());
        _productRepoMock.Setup(r => r.GetByIdAsync(id, default)).ReturnsAsync(product);

        var result = await _sut.GetByIdAsync(id);

        result.IsError.Should().BeFalse();
        result.Value.Should().Be(product.ToDto());
    }

    [Fact]
    public async Task GetByIdAsync_WhenProductNotFound_ReturnsNotFoundError()
    {
        var id = Guid.NewGuid();
        _productRepoMock.Setup(r => r.GetByIdAsync(id, default)).ReturnsAsync((Product?)null);

        var result = await _sut.GetByIdAsync(id);

        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task AddAsync_WhenDataIsCorrect_ReturnsCreatedResponseDtoAndDiscardsImageUrl()
    {
        var categoryId = Guid.NewGuid();
        ProductAddDto addDto = new(
            categoryId,
            "Test",
            "Test description",
            "https://example.com/ignored.png"
        );

        Product? captured = null;
        _productRepoMock
            .Setup(r => r.SlugExistsAsync(It.IsAny<string>(), It.IsAny<Guid?>(), default))
            .ReturnsAsync(false);
        _categoryRepoMock
            .Setup(r => r.GetByIdAsync(categoryId, default))
            .ReturnsAsync(
                new Category
                {
                    Id = categoryId,
                    Name = "Cat",
                    Slug = "cat",
                    Description = "d",
                    SortOrder = 0,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    Products = [],
                }
            );
        _productRepoMock.Setup(r => r.CountInCategoryAsync(categoryId, default)).ReturnsAsync(0);
        _productRepoMock
            .Setup(r => r.Add(It.IsAny<Product>()))
            .Callback<Product>(p => captured = p);
        var result = await _sut.AddAsync(addDto);

        result.IsError.Should().BeFalse();
        _productRepoMock.Verify(r => r.Add(It.IsAny<Product>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(default), Times.Once);

        captured.Should().NotBeNull();
        captured!.Slug.Should().Be("test");
        captured.IsActive.Should().BeTrue();
        captured.SortOrder.Should().Be(0);
        captured.ImageUrl.Should().BeNull();
        result.Value.Should().Be(captured.ToDto());
    }

    [Fact]
    public async Task AddAsync_WhenProductsExistInCategory_SortOrderIsCategoryProductCount()
    {
        var categoryId = Guid.NewGuid();
        ProductAddDto addDto = new(categoryId, "Test", "Test description", null);

        Product? captured = null;
        _productRepoMock
            .Setup(r => r.SlugExistsAsync(It.IsAny<string>(), It.IsAny<Guid?>(), default))
            .ReturnsAsync(false);
        _categoryRepoMock
            .Setup(r => r.GetByIdAsync(categoryId, default))
            .ReturnsAsync(
                new Category
                {
                    Id = categoryId,
                    Name = "Cat",
                    Slug = "cat",
                    Description = "d",
                    SortOrder = 0,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    Products = [],
                }
            );
        _productRepoMock.Setup(r => r.CountInCategoryAsync(categoryId, default)).ReturnsAsync(3);
        _productRepoMock
            .Setup(r => r.Add(It.IsAny<Product>()))
            .Callback<Product>(p => captured = p);

        var result = await _sut.AddAsync(addDto);

        result.IsError.Should().BeFalse();
        captured.Should().NotBeNull();
        captured!.SortOrder.Should().Be(3);
    }

    [Fact]
    public async Task AddAsync_WhenSlugAlreadyExists_ReturnsConflictAndDoesNotPersist()
    {
        var categoryId = Guid.NewGuid();
        ProductAddDto addDto = new(categoryId, "Test", "Test description", null);

        _productRepoMock
            .Setup(r => r.SlugExistsAsync("test", It.IsAny<Guid?>(), default))
            .ReturnsAsync(true);

        var result = await _sut.AddAsync(addDto);

        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Conflict);
        _categoryRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), default), Times.Never);
        _productRepoMock.Verify(r => r.Add(It.IsAny<Product>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(default), Times.Never);
    }

    [Fact]
    public async Task AddAsync_WhenCategoryNotFound_ReturnsNotFoundAndDoesNotPersist()
    {
        var categoryId = Guid.NewGuid();
        ProductAddDto addDto = new(categoryId, "Test", "Test description", null);

        _productRepoMock
            .Setup(r => r.SlugExistsAsync(It.IsAny<string>(), It.IsAny<Guid?>(), default))
            .ReturnsAsync(false);
        _categoryRepoMock
            .Setup(r => r.GetByIdAsync(categoryId, default))
            .ReturnsAsync((Category?)null);

        var result = await _sut.AddAsync(addDto);

        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.NotFound);
        _productRepoMock.Verify(r => r.Add(It.IsAny<Product>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(default), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenProductNotFound_ReturnsNotFoundError()
    {
        var id = Guid.NewGuid();
        _productRepoMock.Setup(r => r.GetByIdAsync(id, default)).ReturnsAsync((Product?)null);
        ProductUpdateDto updateDto = new(null, null, null, null, null, null);

        var result = await _sut.UpdateAsync(id, updateDto);

        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.NotFound);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(default), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenNameChangedToUniqueValue_UpdatesNameAndSlugAndSaves()
    {
        var id = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var product = CreateProduct(id, categoryId, "Old Name", "old-name");
        _productRepoMock.Setup(r => r.GetByIdAsync(id, default)).ReturnsAsync(product);
        _productRepoMock.Setup(r => r.SlugExistsAsync("new-name", id, default)).ReturnsAsync(false);
        ProductUpdateDto updateDto = new("New Name", null, null, null, null, null);

        var result = await _sut.UpdateAsync(id, updateDto);

        result.IsError.Should().BeFalse();
        product.Name.Should().Be("New Name");
        product.Slug.Should().Be("new-name");
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenNameChangedToConflictingSlug_ReturnsConflictAndDoesNotSave()
    {
        var id = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var product = CreateProduct(id, categoryId, "Old Name", "old-name");
        _productRepoMock.Setup(r => r.GetByIdAsync(id, default)).ReturnsAsync(product);
        _productRepoMock.Setup(r => r.SlugExistsAsync("new-name", id, default)).ReturnsAsync(true);
        ProductUpdateDto updateDto = new("New Name", null, null, null, null, null);

        var result = await _sut.UpdateAsync(id, updateDto);

        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Conflict);
        product.Name.Should().Be("Old Name");
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(default), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenOptionalFieldsAreNull_KeepsExistingValues()
    {
        var id = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var product = CreateProduct(id, categoryId, "Name", "name");
        _productRepoMock.Setup(r => r.GetByIdAsync(id, default)).ReturnsAsync(product);
        ProductUpdateDto updateDto = new(null, null, null, null, null, null);

        var result = await _sut.UpdateAsync(id, updateDto);

        result.IsError.Should().BeFalse();
        product.Name.Should().Be("Name");
        product.Slug.Should().Be("name");
        product.Description.Should().Be("description");
        product.CategoryId.Should().Be(categoryId);
        product.IsActive.Should().BeTrue();
        _productRepoMock.Verify(
            r => r.GetAllProductsInCategory(It.IsAny<Guid>(), default),
            Times.Never
        );
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenDescriptionAndIsActiveProvided_UpdatesBothAndSaves()
    {
        var id = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var product = CreateProduct(id, categoryId, "Name", "name", 0, true);
        _productRepoMock.Setup(r => r.GetByIdAsync(id, default)).ReturnsAsync(product);
        ProductUpdateDto updateDto = new(null, null, "New description", null, null, false);

        var result = await _sut.UpdateAsync(id, updateDto);

        result.IsError.Should().BeFalse();
        product.Description.Should().Be("New description");
        product.IsActive.Should().BeFalse();
        product.Name.Should().Be("Name");
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenCategoryIdChangedToExistingCategory_MovesProductAndReordersBothCategoriesAndSaves()
    {
        var id = Guid.NewGuid();
        var oldCategoryId = Guid.NewGuid();
        var newCategoryId = Guid.NewGuid();
        var product = CreateProduct(id, oldCategoryId, "Moved", "moved", 0);
        var remainingInOld = CreateProduct(Guid.NewGuid(), oldCategoryId, "Stays", "stays", 1);
        var existingInNew = CreateProduct(Guid.NewGuid(), newCategoryId, "Existing", "existing", 0);

        _productRepoMock.Setup(r => r.GetByIdAsync(id, default)).ReturnsAsync(product);
        _categoryRepoMock
            .Setup(r => r.GetByIdAsync(newCategoryId, default))
            .ReturnsAsync(
                new Category
                {
                    Id = newCategoryId,
                    Name = "New",
                    Slug = "new",
                    Description = "d",
                    SortOrder = 0,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    Products = [],
                }
            );
        _productRepoMock
            .Setup(r => r.GetAllProductsInCategory(oldCategoryId, default))
            .ReturnsAsync([product, remainingInOld]);
        _productRepoMock
            .Setup(r => r.GetAllProductsInCategory(newCategoryId, default))
            .ReturnsAsync([existingInNew]);
        ProductUpdateDto updateDto = new(null, newCategoryId, null, null, null, null);

        var result = await _sut.UpdateAsync(id, updateDto);

        result.IsError.Should().BeFalse();
        product.CategoryId.Should().Be(newCategoryId);
        remainingInOld.SortOrder.Should().Be(0);
        existingInNew.SortOrder.Should().Be(0);
        product.SortOrder.Should().Be(1);
        _categoryRepoMock.Verify(r => r.GetByIdAsync(newCategoryId, default), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenCategoryIdChangedToNonExistentCategory_ReturnsNotFoundAndDoesNotSave()
    {
        var id = Guid.NewGuid();
        var oldCategoryId = Guid.NewGuid();
        var newCategoryId = Guid.NewGuid();
        var product = CreateProduct(id, oldCategoryId);
        _productRepoMock.Setup(r => r.GetByIdAsync(id, default)).ReturnsAsync(product);
        _categoryRepoMock
            .Setup(r => r.GetByIdAsync(newCategoryId, default))
            .ReturnsAsync((Category?)null);
        ProductUpdateDto updateDto = new(null, newCategoryId, null, null, null, null);

        var result = await _sut.UpdateAsync(id, updateDto);

        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.NotFound);
        product.CategoryId.Should().Be(oldCategoryId);
        _productRepoMock.Verify(
            r => r.GetAllProductsInCategory(It.IsAny<Guid>(), default),
            Times.Never
        );
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(default), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenCategoryIdEqualsCurrentCategory_DoesNotTriggerMove()
    {
        var id = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var product = CreateProduct(id, categoryId);
        _productRepoMock.Setup(r => r.GetByIdAsync(id, default)).ReturnsAsync(product);
        ProductUpdateDto updateDto = new(null, categoryId, null, null, null, null);

        var result = await _sut.UpdateAsync(id, updateDto);

        result.IsError.Should().BeFalse();
        product.CategoryId.Should().Be(categoryId);
        _categoryRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), default), Times.Never);
        _productRepoMock.Verify(
            r => r.GetAllProductsInCategory(It.IsAny<Guid>(), default),
            Times.Never
        );
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenCategoryIdAndSortOrderBothChanged_ReordersWithinNewCategoryUsingCachedList()
    {
        var id = Guid.NewGuid();
        var oldCategoryId = Guid.NewGuid();
        var newCategoryId = Guid.NewGuid();
        var product = CreateProduct(id, oldCategoryId, "Moved", "moved", 0);
        var existing0 = CreateProduct(Guid.NewGuid(), newCategoryId, "First", "first", 0);
        var existing1 = CreateProduct(Guid.NewGuid(), newCategoryId, "Second", "second", 1);

        _productRepoMock.Setup(r => r.GetByIdAsync(id, default)).ReturnsAsync(product);
        _categoryRepoMock
            .Setup(r => r.GetByIdAsync(newCategoryId, default))
            .ReturnsAsync(
                new Category
                {
                    Id = newCategoryId,
                    Name = "New",
                    Slug = "new",
                    Description = "d",
                    SortOrder = 0,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    Products = [],
                }
            );
        _productRepoMock
            .Setup(r => r.GetAllProductsInCategory(oldCategoryId, default))
            .ReturnsAsync([product]);
        _productRepoMock
            .Setup(r => r.GetAllProductsInCategory(newCategoryId, default))
            .ReturnsAsync([existing0, existing1]);
        ProductUpdateDto updateDto = new(null, newCategoryId, null, null, 0, null);

        var result = await _sut.UpdateAsync(id, updateDto);

        result.IsError.Should().BeFalse();
        product.SortOrder.Should().Be(0);
        existing0.SortOrder.Should().Be(1);
        existing1.SortOrder.Should().Be(2);
        _productRepoMock.Verify(
            r => r.GetAllProductsInCategory(newCategoryId, default),
            Times.Once
        );
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(2, true)]
    [InlineData(5, true)]
    public async Task UpdateAsync_SortOrderBoundary_ReturnsExpectedResult(
        int newIndex,
        bool expectError
    )
    {
        var id = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var product = CreateProduct(id, categoryId, "A", "a", 0);
        var other = CreateProduct(Guid.NewGuid(), categoryId, "B", "b", 1);
        _productRepoMock.Setup(r => r.GetByIdAsync(id, default)).ReturnsAsync(product);
        _productRepoMock
            .Setup(r => r.GetAllProductsInCategory(categoryId, default))
            .ReturnsAsync([product, other]);
        ProductUpdateDto updateDto = new(null, null, null, null, newIndex, null);

        var result = await _sut.UpdateAsync(id, updateDto);

        result.IsError.Should().Be(expectError);
        if (expectError)
        {
            result.FirstError.Type.Should().Be(ErrorType.Validation);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(default), Times.Never);
        }
        else
        {
            product.SortOrder.Should().Be(newIndex);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(default), Times.Once);
        }
    }

    [Fact]
    public async Task DeleteAsync_WhenProductNotFound_ReturnsNotFoundError()
    {
        var id = Guid.NewGuid();
        _productRepoMock.Setup(r => r.GetByIdAsync(id, default)).ReturnsAsync((Product?)null);

        var result = await _sut.DeleteAsync(id);

        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.NotFound);
        _productRepoMock.Verify(r => r.Remove(It.IsAny<Product>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(default), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WhenProductExists_RemovesAndReindexesRemainingProductsInCategoryAndSaves()
    {
        var id = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var product = CreateProduct(id, categoryId, "A", "a", 0);
        var remaining1 = CreateProduct(Guid.NewGuid(), categoryId, "B", "b", 1);
        var remaining2 = CreateProduct(Guid.NewGuid(), categoryId, "C", "c", 2);
        _productRepoMock.Setup(r => r.GetByIdAsync(id, default)).ReturnsAsync(product);
        _productRepoMock
            .Setup(r => r.GetAllProductsInCategory(categoryId, default))
            .ReturnsAsync([product, remaining1, remaining2]);

        var result = await _sut.DeleteAsync(id);

        result.IsError.Should().BeFalse();
        _productRepoMock.Verify(r => r.Remove(product), Times.Once);
        remaining1.SortOrder.Should().Be(0);
        remaining2.SortOrder.Should().Be(1);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenProductExists_DoesNotAffectProductsInOtherCategories()
    {
        var id = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var otherCategoryId = Guid.NewGuid();
        var product = CreateProduct(id, categoryId, "A", "a", 0);
        var otherCategoryProduct = CreateProduct(Guid.NewGuid(), otherCategoryId, "Z", "z", 0);
        _productRepoMock.Setup(r => r.GetByIdAsync(id, default)).ReturnsAsync(product);
        _productRepoMock
            .Setup(r => r.GetAllProductsInCategory(categoryId, default))
            .ReturnsAsync([product]);

        var result = await _sut.DeleteAsync(id);

        result.IsError.Should().BeFalse();
        _productRepoMock.Verify(
            r => r.GetAllProductsInCategory(otherCategoryId, default),
            Times.Never
        );
        otherCategoryProduct.SortOrder.Should().Be(0);
        _productRepoMock.Verify(r => r.Remove(otherCategoryProduct), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }
}
