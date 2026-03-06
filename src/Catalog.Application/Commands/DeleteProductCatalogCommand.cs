using Shared.BuildingBlocks.Cqrs;

namespace Catalog.Application;

public sealed record DeleteProductCatalogCommand(Guid ProductId) : ICommand<bool>;
