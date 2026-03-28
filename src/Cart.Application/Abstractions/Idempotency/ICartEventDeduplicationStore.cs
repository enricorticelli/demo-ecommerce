using Shared.BuildingBlocks.Contracts.Messaging;

namespace Cart.Application.Abstractions.Idempotency;

public interface ICartEventDeduplicationStore : IIntegrationEventDeduplicationStore
{
}
