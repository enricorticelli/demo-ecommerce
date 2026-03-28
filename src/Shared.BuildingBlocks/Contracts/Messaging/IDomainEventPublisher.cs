using Shared.BuildingBlocks.Contracts.IntegrationEvents;

namespace Shared.BuildingBlocks.Contracts.Messaging;

public interface IDomainEventPublisher
{
    Task PublishAndFlushAsync(IntegrationEventBase integrationEvent, CancellationToken cancellationToken);
    Task PublishBatchAndFlushAsync(IReadOnlyCollection<IntegrationEventBase> integrationEvents, CancellationToken cancellationToken);
}
