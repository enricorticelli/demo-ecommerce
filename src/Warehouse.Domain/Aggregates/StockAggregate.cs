namespace Warehouse.Domain;

public sealed class StockAggregate
{
    public Guid Id { get; init; }
    public required string Sku { get; init; }
    public int AvailableQuantity { get; set; }
}
