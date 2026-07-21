using AoraCare.Domain;

namespace AoraCare.Application.Dtos;

public sealed record CreateProductDto(
    Guid? CategoryId,
    string Name,
    string Slug,
    string Description,
    string ImageUrl
);

public sealed record ProductDto(
    Guid Id,
    Guid? CategoryId,
    string Name,
    string Slug,
    string Description,
    string ImageUrl,
    bool IsActive,
    int SortOrder,
    DateTime CreatedAt
);

public static class ProductMappingExtensions
{
    public static ProductDto ToDto(this Product product) =>
        new(
            product.Id,
            product.CategoryId,
            product.Name,
            product.Slug,
            product.Description,
            product.ImageUrl,
            product.IsActive,
            product.SortOrder,
            product.CreatedAt
        );
}
