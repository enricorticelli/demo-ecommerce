namespace Shared.BuildingBlocks.Contracts.IntegrationEvents.Catalog;

public sealed record CollectionCreatedV1(Guid CollectionId, string Name, string Slug, bool IsFeatured, IntegrationEventMetadata Metadata) : IntegrationEventBase(Metadata);
