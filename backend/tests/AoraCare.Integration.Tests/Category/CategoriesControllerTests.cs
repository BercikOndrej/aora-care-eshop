using System.Net;
using System.Net.Http.Json;
using AoraCare.Application.Dtos;
using AoraCare.Domain.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;

namespace AoraCare.Tests.Integration.Category;

[Collection(DatabaseCollection.Name)]
public class CategoriesControllerTests : BaseIntegrationTest
{
    private const string CategoryUrl = @"/categories";
    private readonly string _name = "Test Name";
    private readonly string _description = "Test Description";
    private readonly Guid _id = Guid.NewGuid();

    public CategoriesControllerTests(IntegrationTestFactory factory)
        : base(factory) { }

    [Fact]
    public async Task AddAsync_WhenCategoryIsValid_ReturnsCreatedCategory()
    {
        // Arrange
        var content = new CategoryAddDto(_name, _description, true);

        // Act
        var response = await client.PostAsJsonAsync(CategoryUrl, content);
        var responseContent = await response.Content.ReadFromJsonAsync<CategoryResponseDto>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        responseContent.Should().NotBeNull();
        responseContent.Description.Should().Be(_description);
        responseContent.Name.Should().Be(_name);
        responseContent.Slug.Should().Be("test-name");
        responseContent.IsActive.Should().Be(true);
        responseContent.SortOrder.Should().Be(0);

        // Check availability created category
        var id = responseContent.Id;
        var category = await client.GetFromJsonAsync<CategoryResponseDto>($"{CategoryUrl}/{id}");
        category.Should().NotBeNull();
        category.Id.Should().Be(id);
    }

    [Fact]
    public async Task AddAsync_WhenAnotherCategoriesExist_ReturnsCreatedObjectWithCorrectSortOrder()
    {
        // Arrange
        var originalContent = new CategoryAddDto(_name, _description, false);

        foreach (int index in Enumerable.Range(0, 5))
        {
            var content = new CategoryAddDto(_name + index.ToString(), _description, true);

            await client.PostAsJsonAsync(CategoryUrl, content);
        }

        // Act
        var response = await client.PostAsJsonAsync(CategoryUrl, originalContent);
        var responseContent = await response.Content.ReadFromJsonAsync<CategoryResponseDto>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        responseContent.Should().NotBeNull();
        responseContent.Name.Should().Be(_name);
        responseContent.Slug.Should().Be("test-name");
        responseContent.IsActive.Should().Be(false);
        responseContent.SortOrder.Should().Be(5);
    }

    [Fact]
    public async Task AddAsync_WhenDataIsInvalid_ReturnsBadRequest()
    {
        // Arrange
        var content = new { Name = 5, Description = true };

        // Act
        var response = await client.PostAsJsonAsync(CategoryUrl, content);
        var responseContent = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        responseContent.Should().NotBeNull();
        responseContent.Status.Should().Be(400);
        responseContent.Title.Should().Be("One or more validation errors occurred.");
        responseContent.Extensions["errors"].Should().NotBeNull();
    }

    [Fact]
    public async Task AddAsync_WhenSlugIsNotUnique_ReturnsConflict()
    {
        // Arrange
        var content = new CategoryAddDto(_name, _description, null);

        // Act
        await client.PostAsJsonAsync(CategoryUrl, content);
        var response = await client.PostAsJsonAsync(CategoryUrl, content);
        var responseContent = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        responseContent.Should().NotBeNull();
        responseContent.Status.Should().Be(409);
        responseContent.Title.Should().Be("Conflict");
        responseContent.Detail.Should().Be("Property Name has no unique slug.");
    }

    [Fact]
    public async Task GetByIdAsync_WhenNoCategoryExists_ReturnsNotFound()
    {
        // Act
        var response = await client.GetAsync($"{CategoryUrl}/{_id}");
        var responseContent = await response.Content.ReadFromJsonAsync<CategoryResponseDto>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetByIdAsync_WhenCategoryExist_ReturnsCorrectCategoryProductsIncluded()
    {
        // Arrange
        var content = new CategoryAddDto(_name, _description, true);
        var response = await client.PostAsJsonAsync(CategoryUrl, content);
        var responseContent = await response.Content.ReadFromJsonAsync<CategoryResponseDto>();
        responseContent.Should().NotBeNull();
        var id = responseContent.Id;

        await SeedAsync(db =>
            db.Products.Add(
                new Product
                {
                    Id = _id,
                    CategoryId = id,
                    Name = "Test Product",
                    Slug = "test-product",
                    Description = "Test product description",
                    ImageUrl = "https://example.com/test-product.png",
                    SortOrder = 0,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                }
            )
        );

        // Act
        var getResponse = await client.GetAsync($"{CategoryUrl}/{id}");
        var getResponseContent =
            await getResponse.Content.ReadFromJsonAsync<CategoryDetailResponseDto>();

        // Assert
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        getResponseContent.Should().NotBeNull();
        getResponseContent.Id.Should().Be(id);
        getResponseContent.Products.Should().ContainSingle();
        getResponseContent.Products[0].Id.Should().Be(_id);
        getResponseContent.Products[0].Name.Should().Be("Test Product");
        getResponseContent.Products[0].Slug.Should().Be("test-product");
    }

    [Fact]
    public async Task GetAllAsync_WhenNoCategoryExist_ReturnsEmptyList()
    {
        // Act
        var response = await client.GetAsync(CategoryUrl);
        var content = await response.Content.ReadFromJsonAsync<List<CategoryResponseDto>>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().NotBeNull();
        content.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_WhenCategoriesExist_ReturnCategories()
    {
        // Arrange
        foreach (int index in Enumerable.Range(0, 5))
        {
            var addContent = new CategoryAddDto(_name + index.ToString(), _description, true);

            await client.PostAsJsonAsync(CategoryUrl, addContent);
        }

        // Act
        var response = await client.GetAsync(CategoryUrl);
        var content = await response.Content.ReadFromJsonAsync<List<CategoryResponseDto>>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().NotBeNull();
        content.Should().NotBeEmpty();
        content.Count.Should().Be(5);
        content.Should().AllSatisfy(c => c.Name.Should().Contain(_name));
    }

    [Fact]
    public async Task GetAllActiveAsync_WhenCategoriesExist_ReturnsFilteredCategories()
    {
        // Arrange
        bool active = true;
        foreach (int index in Enumerable.Range(0, 5))
        {
            var addContent = new CategoryAddDto(_name + index.ToString(), _description, active);

            await client.PostAsJsonAsync(CategoryUrl, addContent);
            active = !active;
        }

        // Act
        var response = await client.GetAsync(CategoryUrl);
        var activeResponse = await client.GetAsync($"{CategoryUrl}/active");
        var content = await response.Content.ReadFromJsonAsync<List<CategoryResponseDto>>();
        var activecontent = await activeResponse.Content.ReadFromJsonAsync<
            List<CategoryResponseDto>
        >();

        // Assert
        // Non-active
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().NotBeNull();
        var onlyNonActive = content.Where(c => !c.IsActive).ToList();
        onlyNonActive.Should().NotBeEmpty();
        onlyNonActive.Count.Should().Be(2);
        content
            .Should()
            .AllSatisfy(c =>
            {
                c.Name.Should().Contain(_name);
            });

        // active
        activeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        activecontent.Should().NotBeNull();
        activecontent.Should().NotBeEmpty();
        activecontent.Count.Should().Be(3);
        activecontent
            .Should()
            .AllSatisfy(c =>
            {
                c.Name.Should().Contain(_name);
                c.IsActive.Should().BeTrue();
            });
    }

    [Fact]
    public async Task GetAllActiveAsync_WhenCategoriesExistButNooneIsActive_ReturnsEmptyList()
    {
        // Arrange
        foreach (int index in Enumerable.Range(0, 5))
        {
            var addContent = new CategoryAddDto(_name + index.ToString(), _description, false);

            await client.PostAsJsonAsync(CategoryUrl, addContent);
        }

        // Act
        var response = await client.GetAsync($"{CategoryUrl}/active");
        var content = await response.Content.ReadFromJsonAsync<List<CategoryResponseDto>>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().NotBeNull();
        content.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateAsync_WhenNoCategoryExists_ReturnsNotFound()
    {
        // Arrange
        var content = new CategoryUpdateDto(_name, _description, null, null);

        // Act
        var response = await client.PutAsJsonAsync($"{CategoryUrl}/{_id}", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateAsync_WhenNewSlugIsNotUnique_ReturnsConflict()
    {
        // Arrange
        var existingResponse = await client.PostAsJsonAsync(
            CategoryUrl,
            new CategoryAddDto(_name, _description, true)
        );
        var existing = await existingResponse.Content.ReadFromJsonAsync<CategoryResponseDto>();

        var toUpdateResponse = await client.PostAsJsonAsync(
            CategoryUrl,
            new CategoryAddDto(_name + "2", _description, true)
        );
        var toUpdate = await toUpdateResponse.Content.ReadFromJsonAsync<CategoryResponseDto>();

        var content = new CategoryUpdateDto(existing!.Name, null, null, null);

        // Act
        var response = await client.PutAsJsonAsync($"{CategoryUrl}/{toUpdate!.Id}", content);
        var responseContent = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        responseContent.Should().NotBeNull();
        responseContent.Status.Should().Be(409);
        responseContent.Title.Should().Be("Conflict");
        responseContent.Detail.Should().Be("Property Name has no unique slug.");
    }

    [Fact]
    public async Task UpdateAsync_WhenDataIsInvalid_ReturnsBadRequest()
    {
        // Arrange
        var addResponse = await client.PostAsJsonAsync(
            CategoryUrl,
            new CategoryAddDto(_name, _description, true)
        );
        var added = await addResponse.Content.ReadFromJsonAsync<CategoryResponseDto>();

        var content = new CategoryUpdateDto("", null, null, null);

        // Act
        var response = await client.PutAsJsonAsync($"{CategoryUrl}/{added!.Id}", content);
        var responseContent = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        responseContent.Should().NotBeNull();
        responseContent.Status.Should().Be(400);
        responseContent.Title.Should().Be("One or more validation errors occurred.");
        responseContent.Extensions["errors"].Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateAsync_WhenSortOrdedIsUpdated_ReturnUpdatedObjectWithProperOrdering()
    {
        // Arrange
        var created = new List<CategoryResponseDto>();
        foreach (int index in Enumerable.Range(0, 5))
        {
            var addContent = new CategoryAddDto(_name + index.ToString(), _description, true);
            var addResponse = await client.PostAsJsonAsync(CategoryUrl, addContent);
            created.Add((await addResponse.Content.ReadFromJsonAsync<CategoryResponseDto>())!);
        }

        var moved = created[4];
        var content = new CategoryUpdateDto(null, null, 0, null);

        // Act
        var response = await client.PutAsJsonAsync($"{CategoryUrl}/{moved.Id}", content);
        var responseContent = await response.Content.ReadFromJsonAsync<CategoryResponseDto>();

        var allResponse = await client.GetAsync(CategoryUrl);
        var all = await allResponse.Content.ReadFromJsonAsync<List<CategoryResponseDto>>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        responseContent.Should().NotBeNull();
        responseContent.SortOrder.Should().Be(0);

        all.Should().NotBeNull();
        all.OrderBy(c => c.SortOrder)
            .Select(c => c.SortOrder)
            .Should()
            .BeEquivalentTo(Enumerable.Range(0, 5), options => options.WithStrictOrdering());
        all.Single(c => c.Id == moved.Id).SortOrder.Should().Be(0);
    }

    [Fact]
    public async Task UpdateAsync_WhenNameIsUpdated_SlugIsUpdatedToo()
    {
        // Arrange
        var addResponse = await client.PostAsJsonAsync(
            CategoryUrl,
            new CategoryAddDto(_name, _description, true)
        );
        var added = await addResponse.Content.ReadFromJsonAsync<CategoryResponseDto>();

        var newName = "New Category Name";
        var content = new CategoryUpdateDto(newName, null, null, null);

        // Act
        var response = await client.PutAsJsonAsync($"{CategoryUrl}/{added!.Id}", content);
        var responseContent = await response.Content.ReadFromJsonAsync<CategoryResponseDto>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        responseContent.Should().NotBeNull();
        responseContent.Name.Should().Be(newName);
        responseContent.Slug.Should().Be("new-category-name");
    }

    [Fact]
    public async Task UpdateAsync_WhenCategoryExists_PersistsChanges()
    {
        // Arrange
        var addResponse = await client.PostAsJsonAsync(
            CategoryUrl,
            new CategoryAddDto(_name, _description, true)
        );
        var added = await addResponse.Content.ReadFromJsonAsync<CategoryResponseDto>();

        var newDescription = "Updated Description";
        var content = new CategoryUpdateDto(null, newDescription, null, false);

        // Act
        var updateResponse = await client.PutAsJsonAsync($"{CategoryUrl}/{added!.Id}", content);
        var getResponse = await client.GetAsync(CategoryUrl);
        var all = await getResponse.Content.ReadFromJsonAsync<List<CategoryResponseDto>>();

        // Assert
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        all.Should().NotBeNull();
        var persisted = all.Single(c => c.Id == added.Id);
        persisted.Description.Should().Be(newDescription);
        persisted.IsActive.Should().BeFalse();
        persisted.Name.Should().Be(_name);
    }

    [Fact]
    public async Task DeleteAsync_WhenNoCategoryExists_ReturnsNotFound()
    {
        // Act
        var response = await client.DeleteAsync($"{CategoryUrl}/{_id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteAsync_WhenCategoriesExist_ReturnNoContentAndReorderRestOfCategories()
    {
        // Arrange
        var content = new CategoryAddDto(_name, _description, true);
        var response = await client.PostAsJsonAsync(CategoryUrl, content);
        var id = (await response.Content.ReadFromJsonAsync<CategoryResponseDto>())?.Id;

        // Act
        var deleteResponse = await client.DeleteAsync($"{CategoryUrl}/{id}");
        var responseAfterDelete = await client.GetAsync($"{CategoryUrl}/{id}");

        // Assert
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        responseAfterDelete.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteAsync_WhenCategoriesExist_ReturnNoContentAndRemoveCategory()
    {
        // Arrange
        var created = new List<CategoryResponseDto>();
        foreach (int index in Enumerable.Range(0, 3))
        {
            var addContent = new CategoryAddDto(_name + index.ToString(), _description, true);
            var addResponse = await client.PostAsJsonAsync(CategoryUrl, addContent);
            created.Add((await addResponse.Content.ReadFromJsonAsync<CategoryResponseDto>())!);
        }
        var toDelete = created[1];

        // Act
        var deleteResponse = await client.DeleteAsync($"{CategoryUrl}/{toDelete.Id}");
        var getAllResponse = await client.GetAsync(CategoryUrl);
        var remaining = await getAllResponse.Content.ReadFromJsonAsync<List<CategoryResponseDto>>();

        // Assert
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        remaining.Should().NotBeNull();
        remaining.Should().HaveCount(2);
        remaining.Should().NotContain(c => c.Id == toDelete.Id);
    }
}
