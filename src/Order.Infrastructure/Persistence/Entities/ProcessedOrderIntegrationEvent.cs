namespace Order.Infrastructure.Persistence.Entities;

public sealed class ProcessedOrderIntegrationEvent
{
    public Guid EventId { get; set; }
    public DateTimeOffset ProcessedAtUtc { get; set; }
}
