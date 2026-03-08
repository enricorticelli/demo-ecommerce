using Shared.BuildingBlocks.Contracts.Messaging;

namespace Shipping.Application.Abstractions.Idempotency;

public interface IShippingEventDeduplicationStore : IIntegrationEventDeduplicationStore
{
}
