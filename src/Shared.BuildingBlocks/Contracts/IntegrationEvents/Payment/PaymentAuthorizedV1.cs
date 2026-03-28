namespace Shared.BuildingBlocks.Contracts.IntegrationEvents.Payment;

public sealed record PaymentAuthorizedV1(
    Guid OrderId,
    string TransactionId,
    IntegrationEventMetadata Metadata) : IntegrationEventBase(Metadata);
