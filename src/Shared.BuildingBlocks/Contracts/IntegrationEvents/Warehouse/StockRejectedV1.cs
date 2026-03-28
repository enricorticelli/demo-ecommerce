namespace Shared.BuildingBlocks.Contracts.IntegrationEvents.Warehouse;

public sealed record StockRejectedV1(
    Guid OrderId,
    string Reason,
    IntegrationEventMetadata Metadata) : IntegrationEventBase(Metadata);
