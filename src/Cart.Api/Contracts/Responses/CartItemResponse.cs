namespace Cart.Api.Contracts.Responses;

public sealed record CartItemResponse(Guid ProductId, string Sku, string Name, int Quantity, decimal UnitPrice);
