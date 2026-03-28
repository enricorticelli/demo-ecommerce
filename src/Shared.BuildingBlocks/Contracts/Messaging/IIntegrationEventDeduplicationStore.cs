namespace Shared.BuildingBlocks.Contracts.Messaging;

public interface IIntegrationEventDeduplicationStore
{
    Task<bool> HasProcessedAsync(Guid eventId, CancellationToken cancellationToken);
    Task MarkProcessedAsync(Guid eventId, CancellationToken cancellationToken);
}
