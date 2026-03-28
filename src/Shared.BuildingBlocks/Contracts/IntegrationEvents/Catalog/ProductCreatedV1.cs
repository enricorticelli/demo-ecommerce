namespace Shared.BuildingBlocks.Contracts.IntegrationEvents.Catalog;

public sealed record ProductCreatedV1(Guid ProductId, string Sku, Guid BrandId, Guid CategoryId, IReadOnlyList<Guid> CollectionIds, IntegrationEventMetadata Metadata) : IntegrationEventBase(Metadata);
