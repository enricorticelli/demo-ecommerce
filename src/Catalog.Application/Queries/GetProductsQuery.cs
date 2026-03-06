using Shared.BuildingBlocks.Cqrs;

namespace Catalog.Application;

public sealed record GetProductsQuery : IQuery<IReadOnlyList<ProductView>>;
