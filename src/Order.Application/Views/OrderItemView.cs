namespace Order.Application.Views;

public sealed record OrderItemView(Guid ProductId, string Sku, string Name, int Quantity, decimal UnitPrice);
