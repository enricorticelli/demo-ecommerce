using Shared.BuildingBlocks.Cqrs;

namespace Catalog.Application;

public sealed record DeleteCollectionCatalogCommand(Guid CollectionId) : ICommand<bool>;
