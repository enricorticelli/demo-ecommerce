namespace Payment.Infrastructure.Persistence.Entities;

public sealed class ProcessedPaymentIntegrationEvent
{
    public Guid EventId { get; set; }
    public DateTimeOffset ProcessedAtUtc { get; set; }
}
