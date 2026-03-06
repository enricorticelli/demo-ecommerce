using Shared.BuildingBlocks.Cqrs;

namespace Catalog.Application;

public sealed record CreateCollectionCatalogCommand(CreateCollectionCommand Collection) : ICommand<CollectionView>;
