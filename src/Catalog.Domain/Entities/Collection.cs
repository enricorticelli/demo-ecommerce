namespace Catalog.Domain.Entities;

public sealed class Collection
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsFeatured { get; set; }

    public List<ProductCollection> ProductCollections { get; set; } = [];
}
