namespace Cart.Application.Views;

public sealed record CartItemView(Guid ProductId, string Sku, string Name, int Quantity, decimal UnitPrice);
