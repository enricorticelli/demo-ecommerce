using Shared.BuildingBlocks.Cqrs;

namespace Catalog.Application;

public sealed class CollectionRequestHandlers(ICatalogService catalogService) :
    IQueryHandler<GetCollectionsQuery, IReadOnlyList<CollectionView>>,
    IQueryHandler<GetCollectionByIdQuery, CollectionView?>,
    ICommandHandler<CreateCollectionCatalogCommand, CollectionView>,
    ICommandHandler<UpdateCollectionCatalogCommand, CollectionView?>,
    ICommandHandler<DeleteCollectionCatalogCommand, bool>
{
    public Task<IReadOnlyList<CollectionView>> HandleAsync(GetCollectionsQuery query, CancellationToken cancellationToken)
    {
        return catalogService.GetCollectionsAsync(cancellationToken);
    }

    public Task<CollectionView?> HandleAsync(GetCollectionByIdQuery query, CancellationToken cancellationToken)
    {
        return catalogService.GetCollectionByIdAsync(query.CollectionId, cancellationToken);
    }

    public Task<CollectionView> HandleAsync(CreateCollectionCatalogCommand command, CancellationToken cancellationToken)
    {
        return catalogService.CreateCollectionAsync(command.Collection, cancellationToken);
    }

    public Task<CollectionView?> HandleAsync(UpdateCollectionCatalogCommand command, CancellationToken cancellationToken)
    {
        return catalogService.UpdateCollectionAsync(command.CollectionId, command.Collection, cancellationToken);
    }

    public Task<bool> HandleAsync(DeleteCollectionCatalogCommand command, CancellationToken cancellationToken)
    {
        return catalogService.DeleteCollectionAsync(command.CollectionId, cancellationToken);
    }
}
