namespace aoraCareApi.Domain;

public class Category
{
    public Guid Id { get; set; }

    public ICollection<Product> Products { get; set; }

    public string Name { get; set; }

    public string Slug { get; set; }

    public string Description { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }
}
