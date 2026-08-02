using AoraCare.Domain;
using AoraCare.Domain.Models;

namespace AoraCare.Application.Dtos;

public sealed record ProductAddDto(
    Guid CategoryId,
    string Name,
    string Description,
    string? ImageUrl
);

public sealed record ProductResponseDto(
    Guid Id,
    Guid CategoryId,
    string Name,
    string Slug,
    string Description,
    // ! TODO: upload images
    string? ImageUrl,
    bool IsActive,
    int SortOrder,
    DateTime CreatedAt
);

public sealed record ProductUpdateDto(
    string? Name,
    Guid? CategoryId,
    string? Description,
    string? ImageUrl,
    int? SortOrder,
    bool? IsActive
);

public static class ProductMappingExtensions
{
    public static ProductResponseDto ToDto(this Product product) =>
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
