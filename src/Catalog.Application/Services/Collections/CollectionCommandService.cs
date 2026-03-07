using Catalog.Application.Abstractions.Commands;
using Catalog.Application.Abstractions.Repositories;
using Catalog.Application.Abstractions.Rules;
using Catalog.Application.Views;
using Catalog.Domain.Entities;
using Shared.BuildingBlocks.Contracts.IntegrationEvents;
using Shared.BuildingBlocks.Contracts.IntegrationEvents.Catalog;
using Shared.BuildingBlocks.Contracts.Messaging;
using Shared.BuildingBlocks.Exceptions;
using Shared.BuildingBlocks.Mapping;

namespace Catalog.Application.Services.Collections;

public sealed class CollectionCommandService(
    ICollectionRepository collectionRepository,
    ICollectionRules collectionRules,
    IDomainEventPublisher eventPublisher,
    IViewMapper<CatalogCollection, CollectionView> mapper) : ICollectionCommandService
{
    public async Task<CollectionView> CreateAsync(string name, string slug, string description, bool isFeatured, string correlationId, CancellationToken cancellationToken)
    {
        var normalizedName = name.Trim();
        var normalizedSlug = slug.Trim();
        var normalizedDescription = description.Trim();

        await collectionRules.EnsureSlugIsUniqueAsync(normalizedSlug, null, cancellationToken);

        var collection = new CatalogCollection
        {
            Id = Guid.NewGuid(),
            Name = normalizedName,
            Slug = normalizedSlug,
            Description = normalizedDescription,
            IsFeatured = isFeatured
        };

        collectionRepository.Add(collection);

        var integrationEvent = new CollectionCreatedV1(collection.Id, collection.Name, collection.Slug, collection.IsFeatured, CreateMetadata(correlationId));
        await eventPublisher.PublishAndFlushAsync(integrationEvent, cancellationToken);

        return mapper.Map(collection);
    }

    public async Task<CollectionView> UpdateAsync(Guid id, string name, string slug, string description, bool isFeatured, string correlationId, CancellationToken cancellationToken)
    {
        var collection = await collectionRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundAppException($"Collection '{id}' not found.");

        var normalizedName = name.Trim();
        var normalizedSlug = slug.Trim();
        var normalizedDescription = description.Trim();

        await collectionRules.EnsureSlugIsUniqueAsync(normalizedSlug, id, cancellationToken);

        collection.Name = normalizedName;
        collection.Slug = normalizedSlug;
        collection.Description = normalizedDescription;
        collection.IsFeatured = isFeatured;

        var integrationEvent = new CollectionUpdatedV1(collection.Id, collection.Name, collection.Slug, collection.IsFeatured, CreateMetadata(correlationId));
        await eventPublisher.PublishAndFlushAsync(integrationEvent, cancellationToken);

        return mapper.Map(collection);
    }

    public async Task DeleteAsync(Guid id, string correlationId, CancellationToken cancellationToken)
    {
        var collection = await collectionRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundAppException($"Collection '{id}' not found.");

        await collectionRules.EnsureCanDeleteAsync(id, cancellationToken);

        collectionRepository.Remove(collection);

        var integrationEvent = new CollectionDeletedV1(id, CreateMetadata(correlationId));
        await eventPublisher.PublishAndFlushAsync(integrationEvent, cancellationToken);
    }

    private static IntegrationEventMetadata CreateMetadata(string correlationId)
    {
        return new IntegrationEventMetadata(Guid.NewGuid(), DateTimeOffset.UtcNow, correlationId, "Catalog");
    }
}
