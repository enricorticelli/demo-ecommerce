using Shared.BuildingBlocks.Contracts.Messaging;

namespace Order.Application.Abstractions.Idempotency;

public interface IOrderEventDeduplicationStore : IIntegrationEventDeduplicationStore
{
}
