namespace Communication.Infrastructure.Persistence.Entities;

public sealed class ProcessedCommunicationIntegrationEvent
{
    public Guid EventId { get; set; }
    public DateTimeOffset ProcessedAtUtc { get; set; }
}
