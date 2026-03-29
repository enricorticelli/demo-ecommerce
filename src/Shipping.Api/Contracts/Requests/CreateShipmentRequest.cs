namespace Shipping.Api.Contracts.Requests;

public sealed record CreateShipmentRequest(Guid OrderId, Guid UserId, IReadOnlyList<ShippingItemRequest> Items);
