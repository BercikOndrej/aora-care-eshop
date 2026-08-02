using System.Net;
using System.Net.Http.Json;
using AoraCare.Application.Dtos;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;

namespace AoraCare.Tests.Integration.Products;

/// <summary>
///     Integration tests for <c>ProductsController</c>, run against a real database via <see cref="IntegrationTestFactory"/>.
/// </summary>
[Collection(DatabaseCollection.Name)]
public class ProductsControllerTests : BaseIntegrationTest
{
    private const string ProductUrl = @"/products";
    private const string CategoryUrl = @"/categories";
    private readonly string _name = "Test Name";
    private readonly string _description = "Test Description";
    private readonly Guid _id = Guid.NewGuid();

    public ProductsControllerTests(IntegrationTestFactory factory)
        : base(factory) { }

    /// <summary>
    ///     Creates a category through the API for use as a product's parent. Every product needs a real category,
    ///     since <see cref="AoraCare.Domain.Repositories.Interfaces.ICategoryRepository"/> enforces the foreign key.
    /// </summary>
    /// <param name="name">
    ///     Name for the category. Defaults to a unique value so tests can create multiple categories without slug conflicts.
    /// </param>
    /// <param name="isActive">
    ///     Whether the created category is active.
    /// </param>
    /// <returns>
    ///     The created <see cref="CategoryResponseDto"/>.
    /// </returns>
    private async Task<CategoryResponseDto> CreateCategoryAsync(
        string? name = null,
        bool isActive = true
    )
    {
        var response = await client.PostAsJsonAsync(
            CategoryUrl,
            new CategoryAddDto(name ?? _name + Guid.NewGuid(), _description, isActive)
        );
        return (await response.Content.ReadFromJsonAsync<CategoryResponseDto>())!;
    }

    /// <summary>
    ///     Adding a valid product returns 201 with the expected fields, discards the provided ImageUrl,
    ///     and the product is retrievable afterwards.
    /// </summary>
    [Fact]
    public async Task AddAsync_WhenProductIsValid_ReturnsCreatedProduct()
    {
        // Arrange
        var category = await CreateCategoryAsync();
        var content = new ProductAddDto(
            category.Id,
            _name,
            _description,
            "https://example.com/ignored.png"
        );

        // Act
        var response = await client.PostAsJsonAsync(ProductUrl, content);
        var responseContent = await response.Content.ReadFromJsonAsync<ProductResponseDto>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        responseContent.Should().NotBeNull();
        responseContent.CategoryId.Should().Be(category.Id);
        responseContent.Name.Should().Be(_name);
        responseContent.Slug.Should().Be("test-name");
        responseContent.Description.Should().Be(_description);
        responseContent.ImageUrl.Should().BeNull();
        responseContent.IsActive.Should().Be(true);
        responseContent.SortOrder.Should().Be(0);

        // Check availability of created product
        var id = responseContent.Id;
        var product = await client.GetFromJsonAsync<ProductResponseDto>($"{ProductUrl}/{id}");
        product.Should().NotBeNull();
        product.Id.Should().Be(id);
    }

    /// <summary>
    ///     A new product is appended to the end of its category's ordering, regardless of how many products already exist there.
    /// </summary>
    [Fact]
    public async Task AddAsync_WhenOtherProductsExistInCategory_ReturnsCreatedObjectWithCorrectSortOrder()
    {
        // Arrange
        var category = await CreateCategoryAsync();
        foreach (int index in Enumerable.Range(0, 5))
        {
            var content = new ProductAddDto(
                category.Id,
                _name + index.ToString(),
                _description,
                null
            );

            await client.PostAsJsonAsync(ProductUrl, content);
        }

        // Act
        var response = await client.PostAsJsonAsync(
            ProductUrl,
            new ProductAddDto(category.Id, _name, _description, null)
        );
        var responseContent = await response.Content.ReadFromJsonAsync<ProductResponseDto>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        responseContent.Should().NotBeNull();
        responseContent.SortOrder.Should().Be(5);
    }

    /// <summary>
    ///     Adding a product with invalid data returns 400 with validation errors.
    /// </summary>
    [Fact]
    public async Task AddAsync_WhenDataIsInvalid_ReturnsBadRequest()
    {
        // Arrange
        var category = await CreateCategoryAsync();
        var content = new { CategoryId = category.Id, Name = 5, Description = true };

        // Act
        var response = await client.PostAsJsonAsync(ProductUrl, content);
        var responseContent = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        responseContent.Should().NotBeNull();
        responseContent.Status.Should().Be(400);
        responseContent.Title.Should().Be("One or more validation errors occurred.");
        responseContent.Extensions["errors"].Should().NotBeNull();
    }

    /// <summary>
    ///     Adding a product whose name produces a slug that already exists returns 409.
    /// </summary>
    [Fact]
    public async Task AddAsync_WhenSlugIsNotUnique_ReturnsConflict()
    {
        // Arrange
        var category = await CreateCategoryAsync();
        var content = new ProductAddDto(category.Id, _name, _description, null);

        // Act
        await client.PostAsJsonAsync(ProductUrl, content);
        var response = await client.PostAsJsonAsync(ProductUrl, content);
        var responseContent = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        responseContent.Should().NotBeNull();
        responseContent.Status.Should().Be(409);
        responseContent.Title.Should().Be("Conflict");
        responseContent.Detail.Should().Be($"Property {_name} is not unique.");
    }

    /// <summary>
    ///     Adding a product referencing a non-existent category returns 404.
    /// </summary>
    [Fact]
    public async Task AddAsync_WhenCategoryDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var content = new ProductAddDto(_id, _name, _description, null);

        // Act
        var response = await client.PostAsJsonAsync(ProductUrl, content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    /// <summary>
    ///     Fetching a product by an id that does not exist returns 404.
    /// </summary>
    [Fact]
    public async Task GetByIdAsync_WhenNoProductExists_ReturnsNotFound()
    {
        // Act
        var response = await client.GetAsync($"{ProductUrl}/{_id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    /// <summary>
    ///     Fetching an existing product by id returns 200 with its persisted fields.
    /// </summary>
    [Fact]
    public async Task GetByIdAsync_WhenProductExists_ReturnsCorrectProduct()
    {
        // Arrange
        var category = await CreateCategoryAsync();
        var addResponse = await client.PostAsJsonAsync(
            ProductUrl,
            new ProductAddDto(category.Id, _name, _description, null)
        );
        var added = await addResponse.Content.ReadFromJsonAsync<ProductResponseDto>();

        // Act
        var response = await client.GetAsync($"{ProductUrl}/{added!.Id}");
        var responseContent = await response.Content.ReadFromJsonAsync<ProductResponseDto>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        responseContent.Should().NotBeNull();
        responseContent.Id.Should().Be(added.Id);
        responseContent.CategoryId.Should().Be(category.Id);
        responseContent.Name.Should().Be(_name);
        responseContent.Slug.Should().Be("test-name");
    }

    /// <summary>
    ///     Listing products when none exist returns 200 with an empty list.
    /// </summary>
    [Fact]
    public async Task GetAllAsync_WhenNoProductExist_ReturnsEmptyList()
    {
        // Act
        var response = await client.GetAsync(ProductUrl);
        var content = await response.Content.ReadFromJsonAsync<List<ProductResponseDto>>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().NotBeNull();
        content.Should().BeEmpty();
    }

    /// <summary>
    ///     Listing products returns every product, including inactive ones (admin endpoint).
    /// </summary>
    [Fact]
    public async Task GetAllAsync_WhenProductsExist_ReturnProducts()
    {
        // Arrange
        var category = await CreateCategoryAsync();
        foreach (int index in Enumerable.Range(0, 5))
        {
            var addContent = new ProductAddDto(
                category.Id,
                _name + index.ToString(),
                _description,
                null
            );

            await client.PostAsJsonAsync(ProductUrl, addContent);
        }

        // Act
        var response = await client.GetAsync(ProductUrl);
        var content = await response.Content.ReadFromJsonAsync<List<ProductResponseDto>>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().NotBeNull();
        content.Should().NotBeEmpty();
        content.Count.Should().Be(5);
        content.Should().AllSatisfy(p => p.Name.Should().Contain(_name));
    }

    /// <summary>
    ///     The active-products endpoint returns only products with IsActive == true, while the admin endpoint still returns all of them.
    /// </summary>
    [Fact]
    public async Task GetAllActiveAsync_WhenProductsExist_ReturnsFilteredProducts()
    {
        // Arrange
        var category = await CreateCategoryAsync();
        var created = new List<ProductResponseDto>();
        foreach (int index in Enumerable.Range(0, 5))
        {
            var addContent = new ProductAddDto(
                category.Id,
                _name + index.ToString(),
                _description,
                null
            );
            var addResponse = await client.PostAsJsonAsync(ProductUrl, addContent);
            created.Add((await addResponse.Content.ReadFromJsonAsync<ProductResponseDto>())!);
        }

        // Deactivate two of the newly created products; the rest stay active by default.
        foreach (var product in created.Take(2))
        {
            await client.PutAsJsonAsync(
                $"{ProductUrl}/{product.Id}",
                new ProductUpdateDto(null, null, null, null, null, false)
            );
        }

        // Act
        var response = await client.GetAsync(ProductUrl);
        var activeResponse = await client.GetAsync($"{ProductUrl}/active");
        var content = await response.Content.ReadFromJsonAsync<List<ProductResponseDto>>();
        var activeContent = await activeResponse.Content.ReadFromJsonAsync<
            List<ProductResponseDto>
        >();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().NotBeNull();
        var onlyNonActive = content.Where(p => !p.IsActive).ToList();
        onlyNonActive.Should().NotBeEmpty();
        onlyNonActive.Count.Should().Be(2);

        activeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        activeContent.Should().NotBeNull();
        activeContent.Should().NotBeEmpty();
        activeContent.Count.Should().Be(3);
        activeContent.Should().AllSatisfy(p => p.IsActive.Should().BeTrue());
    }

    /// <summary>
    ///     The active-products endpoint returns an empty list when every product has been deactivated.
    /// </summary>
    [Fact]
    public async Task GetAllActiveAsync_WhenProductsExistButNooneIsActive_ReturnsEmptyList()
    {
        // Arrange
        var category = await CreateCategoryAsync();
        foreach (int index in Enumerable.Range(0, 5))
        {
            var addContent = new ProductAddDto(
                category.Id,
                _name + index.ToString(),
                _description,
                null
            );
            var addResponse = await client.PostAsJsonAsync(ProductUrl, addContent);
            var added = await addResponse.Content.ReadFromJsonAsync<ProductResponseDto>();

            await client.PutAsJsonAsync(
                $"{ProductUrl}/{added!.Id}",
                new ProductUpdateDto(null, null, null, null, null, false)
            );
        }

        // Act
        var response = await client.GetAsync($"{ProductUrl}/active");
        var content = await response.Content.ReadFromJsonAsync<List<ProductResponseDto>>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().NotBeNull();
        content.Should().BeEmpty();
    }

    /// <summary>
    ///     Updating a product that does not exist returns 404.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_WhenNoProductExists_ReturnsNotFound()
    {
        // Arrange
        var content = new ProductUpdateDto(_name, null, _description, null, null, null);

        // Act
        var response = await client.PutAsJsonAsync($"{ProductUrl}/{_id}", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    /// <summary>
    ///     Renaming a product to a name whose slug collides with another product returns 409, without persisting the rename.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_WhenNewSlugIsNotUnique_ReturnsConflict()
    {
        // Arrange
        var category = await CreateCategoryAsync();
        var existingResponse = await client.PostAsJsonAsync(
            ProductUrl,
            new ProductAddDto(category.Id, _name, _description, null)
        );
        var existing = await existingResponse.Content.ReadFromJsonAsync<ProductResponseDto>();

        var toUpdateResponse = await client.PostAsJsonAsync(
            ProductUrl,
            new ProductAddDto(category.Id, _name + "2", _description, null)
        );
        var toUpdate = await toUpdateResponse.Content.ReadFromJsonAsync<ProductResponseDto>();

        var content = new ProductUpdateDto(existing!.Name, null, null, null, null, null);

        // Act
        var response = await client.PutAsJsonAsync($"{ProductUrl}/{toUpdate!.Id}", content);
        var responseContent = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        responseContent.Should().NotBeNull();
        responseContent.Status.Should().Be(409);
        responseContent.Title.Should().Be("Conflict");
        responseContent.Detail.Should().Be($"Property {existing.Name} is not unique.");
    }

    /// <summary>
    ///     Updating a product with invalid data returns 400 with validation errors.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_WhenDataIsInvalid_ReturnsBadRequest()
    {
        // Arrange
        var category = await CreateCategoryAsync();
        var addResponse = await client.PostAsJsonAsync(
            ProductUrl,
            new ProductAddDto(category.Id, _name, _description, null)
        );
        var added = await addResponse.Content.ReadFromJsonAsync<ProductResponseDto>();

        var content = new ProductUpdateDto("", null, null, null, null, null);

        // Act
        var response = await client.PutAsJsonAsync($"{ProductUrl}/{added!.Id}", content);
        var responseContent = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        responseContent.Should().NotBeNull();
        responseContent.Status.Should().Be(400);
        responseContent.Title.Should().Be("One or more validation errors occurred.");
        responseContent.Extensions["errors"].Should().NotBeNull();
    }

    /// <summary>
    ///     Moving a product to a new position within its category shifts the other products
    ///     so the whole category ends up with a contiguous 0-based ordering.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_WhenSortOrderIsUpdated_ReturnUpdatedObjectWithProperOrdering()
    {
        // Arrange
        var category = await CreateCategoryAsync();
        var created = new List<ProductResponseDto>();
        foreach (int index in Enumerable.Range(0, 5))
        {
            var addContent = new ProductAddDto(
                category.Id,
                _name + index.ToString(),
                _description,
                null
            );
            var addResponse = await client.PostAsJsonAsync(ProductUrl, addContent);
            created.Add((await addResponse.Content.ReadFromJsonAsync<ProductResponseDto>())!);
        }

        var moved = created[4];
        var content = new ProductUpdateDto(null, null, null, null, 0, null);

        // Act
        var response = await client.PutAsJsonAsync($"{ProductUrl}/{moved.Id}", content);
        var responseContent = await response.Content.ReadFromJsonAsync<ProductResponseDto>();

        var allResponse = await client.GetAsync(ProductUrl);
        var all = await allResponse.Content.ReadFromJsonAsync<List<ProductResponseDto>>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        responseContent.Should().NotBeNull();
        responseContent.SortOrder.Should().Be(0);

        all.Should().NotBeNull();
        all.OrderBy(p => p.SortOrder)
            .Select(p => p.SortOrder)
            .Should()
            .BeEquivalentTo(Enumerable.Range(0, 5), options => options.WithStrictOrdering());
        all.Single(p => p.Id == moved.Id).SortOrder.Should().Be(0);
    }

    /// <summary>
    ///     Renaming a product regenerates its slug from the new name.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_WhenNameIsUpdated_SlugIsUpdatedToo()
    {
        // Arrange
        var category = await CreateCategoryAsync();
        var addResponse = await client.PostAsJsonAsync(
            ProductUrl,
            new ProductAddDto(category.Id, _name, _description, null)
        );
        var added = await addResponse.Content.ReadFromJsonAsync<ProductResponseDto>();

        var newName = "New Product Name";
        var content = new ProductUpdateDto(newName, null, null, null, null, null);

        // Act
        var response = await client.PutAsJsonAsync($"{ProductUrl}/{added!.Id}", content);
        var responseContent = await response.Content.ReadFromJsonAsync<ProductResponseDto>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        responseContent.Should().NotBeNull();
        responseContent.Name.Should().Be(newName);
        responseContent.Slug.Should().Be("new-product-name");
    }

    /// <summary>
    ///     Updated fields (description, IsActive) are actually persisted and visible on a subsequent fetch,
    ///     while omitted fields (name) are left unchanged.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_WhenProductExists_PersistsChanges()
    {
        // Arrange
        var category = await CreateCategoryAsync();
        var addResponse = await client.PostAsJsonAsync(
            ProductUrl,
            new ProductAddDto(category.Id, _name, _description, null)
        );
        var added = await addResponse.Content.ReadFromJsonAsync<ProductResponseDto>();

        var newDescription = "Updated Description";
        var content = new ProductUpdateDto(null, null, newDescription, null, null, false);

        // Act
        var updateResponse = await client.PutAsJsonAsync($"{ProductUrl}/{added!.Id}", content);
        var getResponse = await client.GetAsync($"{ProductUrl}/{added.Id}");
        var persisted = await getResponse.Content.ReadFromJsonAsync<ProductResponseDto>();

        // Assert
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        persisted.Should().NotBeNull();
        persisted.Description.Should().Be(newDescription);
        persisted.IsActive.Should().BeFalse();
        persisted.Name.Should().Be(_name);
    }

    /// <summary>
    ///     Moving a product to a non-existent category returns 404 and leaves the product untouched.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_WhenCategoryIdChangedToNonExistentCategory_ReturnsNotFound()
    {
        // Arrange
        var category = await CreateCategoryAsync();
        var addResponse = await client.PostAsJsonAsync(
            ProductUrl,
            new ProductAddDto(category.Id, _name, _description, null)
        );
        var added = await addResponse.Content.ReadFromJsonAsync<ProductResponseDto>();

        var content = new ProductUpdateDto(null, _id, null, null, null, null);

        // Act
        var response = await client.PutAsJsonAsync($"{ProductUrl}/{added!.Id}", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    /// <summary>
    ///     Moving a product to another category re-indexes the leftover products in the old category
    ///     and appends the moved product to the end of the new category's ordering.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_WhenCategoryIdChanged_MovesProductAndReordersBothCategories()
    {
        // Arrange
        var oldCategory = await CreateCategoryAsync();
        var newCategory = await CreateCategoryAsync();

        var movedResponse = await client.PostAsJsonAsync(
            ProductUrl,
            new ProductAddDto(oldCategory.Id, _name, _description, null)
        );
        var moved = await movedResponse.Content.ReadFromJsonAsync<ProductResponseDto>();

        var remainingResponse = await client.PostAsJsonAsync(
            ProductUrl,
            new ProductAddDto(oldCategory.Id, _name + "-remaining", _description, null)
        );
        var remaining = await remainingResponse.Content.ReadFromJsonAsync<ProductResponseDto>();

        var existingResponse = await client.PostAsJsonAsync(
            ProductUrl,
            new ProductAddDto(newCategory.Id, _name + "-existing", _description, null)
        );
        var existing = await existingResponse.Content.ReadFromJsonAsync<ProductResponseDto>();

        var content = new ProductUpdateDto(null, newCategory.Id, null, null, null, null);

        // Act
        var response = await client.PutAsJsonAsync($"{ProductUrl}/{moved!.Id}", content);
        var responseContent = await response.Content.ReadFromJsonAsync<ProductResponseDto>();

        var remainingGet = await client.GetAsync($"{ProductUrl}/{remaining!.Id}");
        var remainingContent = await remainingGet.Content.ReadFromJsonAsync<ProductResponseDto>();

        var existingGet = await client.GetAsync($"{ProductUrl}/{existing!.Id}");
        var existingContent = await existingGet.Content.ReadFromJsonAsync<ProductResponseDto>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        responseContent.Should().NotBeNull();
        responseContent.CategoryId.Should().Be(newCategory.Id);
        responseContent.SortOrder.Should().Be(1);

        remainingContent.Should().NotBeNull();
        remainingContent.SortOrder.Should().Be(0);

        existingContent.Should().NotBeNull();
        existingContent.SortOrder.Should().Be(0);
    }

    /// <summary>
    ///     Deleting a product that does not exist returns 404.
    /// </summary>
    [Fact]
    public async Task DeleteAsync_WhenNoProductExists_ReturnsNotFound()
    {
        // Act
        var response = await client.DeleteAsync($"{ProductUrl}/{_id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    /// <summary>
    ///     Deleting a product re-indexes the remaining products in its category to a contiguous 0-based ordering.
    /// </summary>
    [Fact]
    public async Task DeleteAsync_WhenProductsExist_ReturnNoContentAndReorderRestOfProductsInCategory()
    {
        // Arrange
        var category = await CreateCategoryAsync();
        var created = new List<ProductResponseDto>();
        foreach (int index in Enumerable.Range(0, 3))
        {
            var addContent = new ProductAddDto(
                category.Id,
                _name + index.ToString(),
                _description,
                null
            );
            var addResponse = await client.PostAsJsonAsync(ProductUrl, addContent);
            created.Add((await addResponse.Content.ReadFromJsonAsync<ProductResponseDto>())!);
        }
        var toDelete = created[0];

        // Act
        var deleteResponse = await client.DeleteAsync($"{ProductUrl}/{toDelete.Id}");
        var getAllResponse = await client.GetAsync(ProductUrl);
        var remaining = await getAllResponse.Content.ReadFromJsonAsync<List<ProductResponseDto>>();

        // Assert
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        remaining.Should().NotBeNull();
        remaining.Should().HaveCount(2);
        remaining
            .OrderBy(p => p.SortOrder)
            .Select(p => p.SortOrder)
            .Should()
            .BeEquivalentTo(Enumerable.Range(0, 2), options => options.WithStrictOrdering());
    }

    /// <summary>
    ///     Deleting a product removes it and leaves the other products in place.
    /// </summary>
    [Fact]
    public async Task DeleteAsync_WhenProductsExist_ReturnNoContentAndRemoveProduct()
    {
        // Arrange
        var category = await CreateCategoryAsync();
        var created = new List<ProductResponseDto>();
        foreach (int index in Enumerable.Range(0, 3))
        {
            var addContent = new ProductAddDto(
                category.Id,
                _name + index.ToString(),
                _description,
                null
            );
            var addResponse = await client.PostAsJsonAsync(ProductUrl, addContent);
            created.Add((await addResponse.Content.ReadFromJsonAsync<ProductResponseDto>())!);
        }
        var toDelete = created[1];

        // Act
        var deleteResponse = await client.DeleteAsync($"{ProductUrl}/{toDelete.Id}");
        var getAllResponse = await client.GetAsync(ProductUrl);
        var remaining = await getAllResponse.Content.ReadFromJsonAsync<List<ProductResponseDto>>();

        // Assert
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        remaining.Should().NotBeNull();
        remaining.Should().HaveCount(2);
        remaining.Should().NotContain(p => p.Id == toDelete.Id);
    }
}
