using AoraCare.Application.Dtos;
using AoraCare.Application.Services.Interfaces;
using AoraCare.Domain;
using AoraCare.Domain.Common;
using AoraCare.Domain.Models;
using AoraCare.Domain.Repositories.Interfaces;
using ErrorOr;
using Microsoft.Extensions.Logging;

namespace AoraCare.Application.Services;

public class ProductService : IProductService
{
    private readonly IUnitOfWork _uow;
    private readonly IProductRepository _productRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly ILogger<ProductService> _logger;

    public ProductService(
        ILogger<ProductService> logger,
        IUnitOfWork uow,
        IProductRepository productRepo,
        ICategoryRepository categoryRepo
    )
    {
        _logger = logger;
        _uow = uow;
        _productRepository = productRepo;
        _categoryRepository = categoryRepo;
    }

    public async Task<ErrorOr<ProductResponseDto>> AddAsync(
        ProductAddDto addDto,
        CancellationToken ct = default
    )
    {
        string slug = SlugHelper.CreateSlug(addDto.Name);

        if (!await IsSlugUnique(slug, ct: ct))
        {
            _logger.LogWarning("Property {name} is not unique. Try another name", addDto.Name);
            return Error.Conflict(description: $"Property {addDto.Name} is not unique.");
        }

        var category = await _categoryRepository.GetByIdAsync(addDto.CategoryId, ct);
        if (category is null)
        {
            _logger.LogWarning("No category with {id} not found.", addDto.CategoryId);
            return Error.NotFound($"Category with {addDto.CategoryId} not found.");
        }

        Product newProduct = new Product
        {
            Id = Guid.NewGuid(),
            CategoryId = addDto.CategoryId,
            Name = addDto.Name,
            Slug = slug,
            Description = addDto.Description,
            // ! TODO: upload images
            ImageUrl = null,
            SortOrder = await GetNextOrder(addDto.CategoryId, ct),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        _productRepository.Add(newProduct);
        await _uow.SaveChangesAsync(ct);
        return newProduct.ToDto();
    }

    public async Task<ErrorOr<Deleted>> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var found = await _productRepository.GetByIdAsync(id, ct);

        if (found is null)
        {
            _logger.LogWarning("Product with {id} not found.", id);
            return Error.NotFound(description: $"Product with {id} not found.");
        }

        var orderedInCategory = await _productRepository.GetAllProductsInCategory(
            found.CategoryId,
            ct
        );

        _productRepository.Remove(found);
        orderedInCategory.Remove(found);
        ReorderAllProducts(orderedInCategory);

        await _uow.SaveChangesAsync(ct);
        return Result.Deleted;
    }

    public async Task<List<ProductResponseDto>> GetAllActiveAsync(CancellationToken ct = default) =>
        (await _productRepository.GetAllAsync(ct))
            .Where(p => p.IsActive)
            .Select(p => p.ToDto())
            .ToList();

    public async Task<List<ProductResponseDto>> GetAllAsync(CancellationToken ct = default) =>
        (await _productRepository.GetAllAsync(ct)).Select(p => p.ToDto()).ToList();

    public async Task<ErrorOr<ProductResponseDto>> GetByIdAsync(
        Guid id,
        CancellationToken ct = default
    )
    {
        var product = await _productRepository.GetByIdAsync(id, ct);

        if (product is null)
        {
            _logger.LogWarning("Product with {id} not found.", id);
            return Error.NotFound($"Product with {id} not found.");
        }
        return product.ToDto();
    }

    public async Task<ErrorOr<ProductResponseDto>> UpdateAsync(
        Guid id,
        ProductUpdateDto data,
        CancellationToken ct = default
    )
    {
        var old = await _productRepository.GetByIdAsync(id, ct);
        if (old is null)
        {
            _logger.LogWarning("Product with {id} not found.", id);
            return Error.NotFound($"Product with {id} not found");
        }

        if (data.Name is not null)
        {
            string slug = SlugHelper.CreateSlug(data.Name);

            if (!await IsSlugUnique(slug, id, ct: ct))
            {
                _logger.LogWarning("Property {name} is not unique. Try another name", data.Name);
                return Error.Conflict(description: $"Property {data.Name} is not unique.");
            }

            old.Slug = slug;
        }

        old.Name = data.Name ?? old.Name;
        old.Description = data.Description ?? old.Description;
        // ! TODO: upload images
        old.ImageUrl = null;
        old.IsActive = data.IsActive ?? old.IsActive;

        List<Product>? currentCategoryProducts = null;

        if (data.CategoryId.HasValue && data.CategoryId.Value != old.CategoryId)
        {
            var newCategoryId = data.CategoryId.Value;
            var category = await _categoryRepository.GetByIdAsync(newCategoryId, ct);
            if (category is null)
            {
                _logger.LogWarning("No category with {id} not found.", newCategoryId);
                return Error.NotFound($"Category with {newCategoryId} not found.");
            }
            var oldCategoryProducts = await _productRepository.GetAllProductsInCategory(
                old.CategoryId,
                ct
            );
            var newCategoryProducts = await _productRepository.GetAllProductsInCategory(
                newCategoryId,
                ct
            );

            oldCategoryProducts.Remove(old);
            newCategoryProducts.Add(old);
            ReorderAllProducts(oldCategoryProducts);
            ReorderAllProducts(newCategoryProducts);

            old.CategoryId = newCategoryId;
            currentCategoryProducts = newCategoryProducts;
            _logger.LogInformation(
                "Product with {id} has been moved into Category {categoryId}",
                old.Id,
                old.CategoryId
            );
        }

        if (data.SortOrder.HasValue)
        {
            var productsInCategory =
                currentCategoryProducts
                ?? await _productRepository.GetAllProductsInCategory(old.CategoryId, ct);
            var count = productsInCategory.Count;
            int newIndex = data.SortOrder.Value;

            if (newIndex >= count)
            {
                _logger.LogWarning(
                    "SortOrder value ({index}) is out of index range. Value cannot be greater than number of products in category ({count}).",
                    newIndex,
                    count
                );
                return Error.Validation(
                    description: $"SortOrder value ({newIndex}) is out of index range. Value cannot be greater than number of products in category ({count})."
                );
            }

            productsInCategory.Remove(old);
            productsInCategory.Insert(newIndex, old);
            ReorderAllProducts(productsInCategory);
        }

        await _uow.SaveChangesAsync(ct);
        return old.ToDto();
    }

    #region Private Methods

    private async Task<bool> IsSlugUnique(
        string slug,
        Guid? id = null,
        CancellationToken ct = default
    ) => !await _productRepository.SlugExistsAsync(slug, id, ct);

    private Task<int> GetNextOrder(Guid categoryId, CancellationToken ct = default) =>
        _productRepository.CountInCategoryAsync(categoryId, ct);

    private void ReorderAllProducts(List<Product> products)
    {
        for (int i = 0; i < products.Count; i++)
        {
            products[i].SortOrder = i;
        }
    }

    #endregion
}
