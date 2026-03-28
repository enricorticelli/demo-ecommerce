using Microsoft.Extensions.Logging;
using Shared.BuildingBlocks.Contracts.IntegrationEvents;
using Shared.BuildingBlocks.Contracts.Messaging;

namespace Shared.BuildingBlocks.Messaging;

public abstract class IntegrationEventHandlerBase<TIntegrationEvent>(
    IIntegrationEventDeduplicationStore deduplicationStore,
    ILogger logger)
    where TIntegrationEvent : IIntegrationEvent
{
    protected async Task HandleDeduplicatedAsync(
        TIntegrationEvent integrationEvent,
        Func<CancellationToken, Task> handleCoreAsync,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Handling {EventType}. eventId={EventId} correlationId={CorrelationId}",
            typeof(TIntegrationEvent).Name,
            integrationEvent.Metadata.EventId,
            integrationEvent.Metadata.CorrelationId);

        if (await deduplicationStore.HasProcessedAsync(integrationEvent.Metadata.EventId, cancellationToken))
        {
            logger.LogInformation(
                "Skipping duplicated {EventType}. eventId={EventId}",
                typeof(TIntegrationEvent).Name,
                integrationEvent.Metadata.EventId);
            return;
        }

        await handleCoreAsync(cancellationToken);
        await deduplicationStore.MarkProcessedAsync(integrationEvent.Metadata.EventId, cancellationToken);

        logger.LogInformation(
            "Completed {EventType}. eventId={EventId}",
            typeof(TIntegrationEvent).Name,
            integrationEvent.Metadata.EventId);
    }

    protected static IntegrationEventMetadata CreateMetadata(string correlationId, string sourceContext)
    {
        return IntegrationEventMetadataFactory.Create(correlationId, sourceContext);
    }
}
