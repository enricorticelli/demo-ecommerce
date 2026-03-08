namespace Shipping.Infrastructure.Persistence.Entities;

public sealed class ProcessedShippingIntegrationEvent
{
    public Guid EventId { get; set; }
    public DateTimeOffset ProcessedAtUtc { get; set; }
}
