namespace Shared.BuildingBlocks.Contracts.IntegrationEvents.Catalog;

public sealed record BrandDeletedV1(Guid BrandId, IntegrationEventMetadata Metadata) : IntegrationEventBase(Metadata);
