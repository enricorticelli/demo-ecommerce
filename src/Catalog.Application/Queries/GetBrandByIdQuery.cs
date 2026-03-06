using Shared.BuildingBlocks.Cqrs;

namespace Catalog.Application;

public sealed record GetBrandByIdQuery(Guid BrandId) : IQuery<BrandView?>;
