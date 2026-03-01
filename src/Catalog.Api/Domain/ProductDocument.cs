namespace Catalog.Api.Domain;

public sealed class ProductDocument
{
    public Guid Id { get; init; }
    public required string Sku { get; init; }
    public required string Name { get; init; }
    public string Description { get; init; } = string.Empty;
    public decimal Price { get; init; }
}
