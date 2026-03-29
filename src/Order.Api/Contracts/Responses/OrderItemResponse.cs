namespace Order.Api.Contracts.Responses;

public sealed record OrderItemResponse(Guid ProductId, string Sku, string Name, int Quantity, decimal UnitPrice);
