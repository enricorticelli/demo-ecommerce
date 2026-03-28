namespace Shared.BuildingBlocks.Contracts.IntegrationEvents.Order;

public sealed record OrderCancelledV1(
    Guid OrderId,
    string Reason,
    IntegrationEventMetadata Metadata) : IntegrationEventBase(Metadata);
