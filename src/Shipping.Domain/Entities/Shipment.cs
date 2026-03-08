using Shared.BuildingBlocks.Exceptions;

namespace Shipping.Domain.Entities;

public sealed class Shipment
{
    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public Guid UserId { get; private set; }
    public string TrackingCode { get; private set; } = string.Empty;
    public string Status { get; private set; } = ShipmentStatus.Preparing;
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public DateTimeOffset? DeliveredAtUtc { get; private set; }

    private Shipment()
    {
    }

    private Shipment(Guid orderId, Guid userId, string trackingCode)
    {
        Id = Guid.NewGuid();
        OrderId = orderId;
        UserId = userId;
        TrackingCode = trackingCode;
        Status = ShipmentStatus.Created;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public static Shipment Create(Guid orderId, Guid userId)
    {
        if (orderId == Guid.Empty)
        {
            throw new ValidationAppException("Order id is required.");
        }

        if (userId == Guid.Empty)
        {
            throw new ValidationAppException("User id is required.");
        }

        var trackingCode = $"TRK-{Guid.NewGuid():N}"[..16];
        return new Shipment(orderId, userId, trackingCode);
    }

    public void UpdateStatus(string status)
    {
        if (!ShipmentStatus.IsValid(status))
        {
            throw new ValidationAppException($"Shipment status '{status}' is invalid.");
        }

        if (string.Equals(Status, status, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        Status = status;
        UpdatedAtUtc = DateTimeOffset.UtcNow;

        if (string.Equals(status, ShipmentStatus.Delivered, StringComparison.OrdinalIgnoreCase))
        {
            DeliveredAtUtc = UpdatedAtUtc;
            return;
        }

        DeliveredAtUtc = null;
    }
}
