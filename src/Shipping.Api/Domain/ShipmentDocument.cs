namespace Shipping.Api.Domain;

public sealed class ShipmentDocument
{
    public Guid Id { get; init; }
    public Guid OrderId { get; init; }
    public Guid UserId { get; init; }
    public required string TrackingCode { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}
