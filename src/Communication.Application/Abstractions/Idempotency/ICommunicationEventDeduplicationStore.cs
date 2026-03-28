using Shared.BuildingBlocks.Contracts.Messaging;

namespace Communication.Application.Abstractions.Idempotency;

public interface ICommunicationEventDeduplicationStore : IIntegrationEventDeduplicationStore
{
}
