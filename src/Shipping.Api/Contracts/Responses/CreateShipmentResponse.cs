namespace Shipping.Api.Contracts.Responses;

public sealed record CreateShipmentResponse(Guid OrderId, string TrackingCode);
