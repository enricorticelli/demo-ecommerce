using Shared.BuildingBlocks.Cqrs;

namespace Catalog.Application;

public sealed record CreateProductCatalogCommand(CreateProductCommand Product) : ICommand<ProductView?>;
