namespace Shipping.Application.Views;

public sealed record ShipmentView(
    Guid Id,
    Guid OrderId,
    Guid UserId,
    string TrackingCode,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? DeliveredAtUtc);
