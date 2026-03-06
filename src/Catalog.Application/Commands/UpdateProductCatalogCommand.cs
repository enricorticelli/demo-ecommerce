using Shared.BuildingBlocks.Cqrs;

namespace Catalog.Application;

public sealed record UpdateProductCatalogCommand(Guid ProductId, UpdateProductCommand Product) : ICommand<ProductView?>;
