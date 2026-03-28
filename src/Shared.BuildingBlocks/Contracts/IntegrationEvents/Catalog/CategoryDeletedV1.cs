namespace Shared.BuildingBlocks.Contracts.IntegrationEvents.Catalog;

public sealed record CategoryDeletedV1(Guid CategoryId, IntegrationEventMetadata Metadata) : IntegrationEventBase(Metadata);
