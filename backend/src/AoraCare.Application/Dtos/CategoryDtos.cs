using AoraCare.Domain;
using AoraCare.Domain.Models;

namespace AoraCare.Application.Dtos;

public sealed record CategoryResponseDto(
    Guid Id,
    string Name,
    string Slug,
    string Description,
    int SortOrder,
    bool IsActive
);

public sealed record CategoryDetailResponseDto(
    Guid Id,
    string Name,
    string Slug,
    string Description,
    int SortOrder,
    bool IsActive,
    IReadOnlyList<ProductDto> Products
);

public sealed record CategoryAddDto(string Name, string Description, bool? IsActive);

public sealed record CategoryUpdateDto(
    string? Name,
    string? Description,
    int? SortOrder,
    bool? IsActive
);

public static class CategoryMappingExtensions
{
    public static CategoryResponseDto ToDto(this Category category) =>
        new(
            category.Id,
            category.Name,
            category.Slug,
            category.Description,
            category.SortOrder,
            category.IsActive
        );

    public static CategoryDetailResponseDto ToDetailDto(this Category category) =>
        new(
            category.Id,
            category.Name,
            category.Slug,
            category.Description,
            category.SortOrder,
            category.IsActive,
            category.Products.Select(p => p.ToDto()).ToList()
        );
}
