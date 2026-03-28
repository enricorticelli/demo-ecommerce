namespace Shared.BuildingBlocks.Contracts.IntegrationEvents.Catalog;

public sealed record CollectionDeletedV1(Guid CollectionId, IntegrationEventMetadata Metadata) : IntegrationEventBase(Metadata);
