namespace Catalog.Domain;

public sealed class ProductAggregate
{
    public Guid Id { get; init; }
    public required string Sku { get; init; }
    public required string Name { get; init; }
    public string Description { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public Guid BrandId { get; init; }
    public Guid CategoryId { get; init; }
    public IReadOnlyList<Guid> CollectionIds { get; init; } = [];
    public bool IsNewArrival { get; init; }
    public bool IsBestSeller { get; init; }
    public DateTimeOffset CreatedAtUtc { get; init; }
}
