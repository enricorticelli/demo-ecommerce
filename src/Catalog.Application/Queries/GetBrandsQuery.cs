using Shared.BuildingBlocks.Cqrs;

namespace Catalog.Application;

public sealed record GetBrandsQuery : IQuery<IReadOnlyList<BrandView>>;
