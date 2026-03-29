namespace Shipping.Api.Contracts.Requests;

public sealed record ShippingItemRequest(Guid ProductId, string Sku, string Name, int Quantity, decimal UnitPrice);
