namespace Shared.BuildingBlocks.Contracts.IntegrationEvents.Catalog;

public sealed record BrandCreatedV1(Guid BrandId, string Name, string Slug, IntegrationEventMetadata Metadata) : IntegrationEventBase(Metadata);
