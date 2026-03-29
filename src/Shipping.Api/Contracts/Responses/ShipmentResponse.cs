namespace Shipping.Api.Contracts.Responses;

public sealed record ShipmentResponse(
    Guid Id,
    Guid OrderId,
    Guid UserId,
    string TrackingCode,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? DeliveredAtUtc);
