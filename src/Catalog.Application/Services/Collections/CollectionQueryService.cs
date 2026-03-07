using Catalog.Application.Abstractions.Queries;
using Catalog.Application.Abstractions.Repositories;
using Catalog.Application.Views;
using Catalog.Domain.Entities;
using Shared.BuildingBlocks.Exceptions;
using Shared.BuildingBlocks.Mapping;

namespace Catalog.Application.Services.Collections;

public sealed class CollectionQueryService(
    ICollectionRepository collectionRepository,
    IViewMapper<CatalogCollection, CollectionView> mapper) : ICollectionQueryService
{
    public async Task<IReadOnlyList<CollectionView>> ListAsync(string? searchTerm, CancellationToken cancellationToken)
    {
        var collections = await collectionRepository.ListAsync(searchTerm, cancellationToken);
        return collections.Select(mapper.Map).ToArray();
    }

    public async Task<CollectionView> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var collection = await collectionRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundAppException($"Collection '{id}' not found.");

        return mapper.Map(collection);
    }
}
