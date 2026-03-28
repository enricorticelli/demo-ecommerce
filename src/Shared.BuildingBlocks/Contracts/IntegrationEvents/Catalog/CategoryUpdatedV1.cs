namespace Shared.BuildingBlocks.Contracts.IntegrationEvents.Catalog;

public sealed record CategoryUpdatedV1(Guid CategoryId, string Name, string Slug, IntegrationEventMetadata Metadata) : IntegrationEventBase(Metadata);
