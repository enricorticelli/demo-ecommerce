using Shared.BuildingBlocks.Cqrs;

namespace Catalog.Application;

public sealed record GetCollectionByIdQuery(Guid CollectionId) : IQuery<CollectionView?>;
