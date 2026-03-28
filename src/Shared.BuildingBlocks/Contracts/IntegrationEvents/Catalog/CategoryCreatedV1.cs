namespace Shared.BuildingBlocks.Contracts.IntegrationEvents.Catalog;

public sealed record CategoryCreatedV1(Guid CategoryId, string Name, string Slug, IntegrationEventMetadata Metadata) : IntegrationEventBase(Metadata);
