namespace Warehouse.Infrastructure.Persistence.Entities;

public sealed class ProcessedWarehouseIntegrationEvent
{
    public Guid EventId { get; set; }
    public DateTimeOffset ProcessedAtUtc { get; set; }
}
