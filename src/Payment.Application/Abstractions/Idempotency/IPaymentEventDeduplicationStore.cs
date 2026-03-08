using Shared.BuildingBlocks.Contracts.Messaging;

namespace Payment.Application.Abstractions.Idempotency;

public interface IPaymentEventDeduplicationStore : IIntegrationEventDeduplicationStore
{
}
