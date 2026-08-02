namespace AoraCare.Domain.Models;

public class Product
{
    public Guid Id { get; set; }

    public Guid CategoryId { get; set; }

    public Category Category { get; set; }

    public string Name { get; set; }

    public string Slug { get; set; }

    public string Description { get; set; }

    // ! TODO: upload images
    public string? ImageUrl { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
