namespace Order.Application.Commands;

public sealed record CreateOrderItemCommand(Guid ProductId, string Sku, string Name, int Quantity, decimal UnitPrice);
