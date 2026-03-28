namespace Shared.BuildingBlocks.Contracts.IntegrationEvents.Payment;

public sealed record PaymentRejectedV1(
    Guid OrderId,
    string Reason,
    IntegrationEventMetadata Metadata) : IntegrationEventBase(Metadata);
