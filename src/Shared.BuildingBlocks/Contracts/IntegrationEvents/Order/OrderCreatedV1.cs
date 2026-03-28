namespace Shared.BuildingBlocks.Contracts.IntegrationEvents.Order;

public sealed record OrderCreatedV1(
    Guid OrderId,
    Guid CartId,
    Guid UserId,
    string PaymentMethod,
    decimal TotalAmount,
    string Status,
    IntegrationEventMetadata Metadata) : IntegrationEventBase(Metadata);
