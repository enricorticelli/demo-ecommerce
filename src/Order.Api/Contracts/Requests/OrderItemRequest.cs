namespace Order.Api.Contracts.Requests;

public sealed record OrderItemRequest(Guid ProductId, string Sku, string Name, int Quantity, decimal UnitPrice);
