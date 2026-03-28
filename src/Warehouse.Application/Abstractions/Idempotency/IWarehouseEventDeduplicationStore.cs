using Shared.BuildingBlocks.Contracts.Messaging;

namespace Warehouse.Application.Abstractions.Idempotency;

public interface IWarehouseEventDeduplicationStore : IIntegrationEventDeduplicationStore
{
}
