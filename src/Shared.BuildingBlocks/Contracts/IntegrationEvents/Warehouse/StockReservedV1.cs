namespace Shared.BuildingBlocks.Contracts.IntegrationEvents.Warehouse;

public sealed record StockReservedV1(
    Guid OrderId,
    IntegrationEventMetadata Metadata) : IntegrationEventBase(Metadata);
