namespace Warehouse.Api.Domain;

public sealed class StockDocument
{
    public Guid Id { get; init; }
    public required string Sku { get; init; }
    public int AvailableQuantity { get; set; }
}
