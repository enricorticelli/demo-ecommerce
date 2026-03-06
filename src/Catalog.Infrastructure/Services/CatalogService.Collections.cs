using Catalog.Application;
using Catalog.Domain;
using Marten;

namespace Catalog.Infrastructure;

public sealed partial class CatalogService
{
    public async Task<IReadOnlyList<CollectionView>> GetCollectionsAsync(CancellationToken cancellationToken)
    {
        var collections = await _querySession.Query<CollectionAggregate>()
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return collections.Select(MapToView).ToArray();
    }

    public async Task<CollectionView?> GetCollectionByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var collection = await _querySession.LoadAsync<CollectionAggregate>(id, cancellationToken);
        return collection is null ? null : MapToView(collection);
    }

    public async Task<CollectionView> CreateCollectionAsync(CreateCollectionCommand command, CancellationToken cancellationToken)
    {
        var collection = new CollectionAggregate
        {
            Id = Guid.NewGuid(),
            Name = command.Name,
            Slug = command.Slug,
            Description = command.Description,
            IsFeatured = command.IsFeatured
        };

        _documentSession.Store(collection);
        await _documentSession.SaveChangesAsync(cancellationToken);
        return MapToView(collection);
    }

    public async Task<CollectionView?> UpdateCollectionAsync(Guid id, UpdateCollectionCommand command, CancellationToken cancellationToken)
    {
        var existing = await _documentSession.LoadAsync<CollectionAggregate>(id, cancellationToken);
        if (existing is null)
        {
            return null;
        }

        var updated = new CollectionAggregate
        {
            Id = id,
            Name = command.Name,
            Slug = command.Slug,
            Description = command.Description,
            IsFeatured = command.IsFeatured
        };

        _documentSession.Store(updated);
        await _documentSession.SaveChangesAsync(cancellationToken);
        return MapToView(updated);
    }

    public async Task<bool> DeleteCollectionAsync(Guid id, CancellationToken cancellationToken)
    {
        var collection = await _documentSession.LoadAsync<CollectionAggregate>(id, cancellationToken);
        if (collection is null)
        {
            return false;
        }

        _documentSession.Delete<CollectionAggregate>(id);
        await _documentSession.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static CollectionView MapToView(CollectionAggregate collection)
    {
        return new CollectionView(collection.Id, collection.Name, collection.Slug, collection.Description, collection.IsFeatured);
    }
}
