using Shared.BuildingBlocks.Cqrs;

namespace Catalog.Application;

public sealed record GetCollectionsQuery : IQuery<IReadOnlyList<CollectionView>>;
