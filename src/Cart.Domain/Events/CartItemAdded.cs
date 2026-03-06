namespace Cart.Domain;

public sealed record CartItemAdded(Guid CartId, Guid ProductId, string Sku, string Name, int Quantity, decimal UnitPrice);
