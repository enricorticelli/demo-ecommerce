using Shared.BuildingBlocks.Cqrs;

namespace Catalog.Application;

public sealed record GetProductByIdQuery(Guid ProductId) : IQuery<ProductView?>;
