namespace Shared.BuildingBlocks.Contracts.IntegrationEvents.Catalog;

public sealed record CollectionUpdatedV1(Guid CollectionId, string Name, string Slug, bool IsFeatured, IntegrationEventMetadata Metadata) : IntegrationEventBase(Metadata);
