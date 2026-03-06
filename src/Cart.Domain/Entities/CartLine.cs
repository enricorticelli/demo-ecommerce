namespace Cart.Domain;

public sealed class CartLine
{
    public Guid ProductId { get; init; }
    public required string Sku { get; init; }
    public required string Name { get; init; }
    public decimal UnitPrice { get; init; }
    public int Quantity { get; set; }
}
