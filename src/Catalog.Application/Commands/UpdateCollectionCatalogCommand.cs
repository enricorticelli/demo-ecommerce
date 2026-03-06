using Shared.BuildingBlocks.Cqrs;

namespace Catalog.Application;

public sealed record UpdateCollectionCatalogCommand(Guid CollectionId, UpdateCollectionCommand Collection) : ICommand<CollectionView?>;
