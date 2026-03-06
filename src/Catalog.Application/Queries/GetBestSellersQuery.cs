using Shared.BuildingBlocks.Cqrs;

namespace Catalog.Application;

public sealed record GetBestSellersQuery : IQuery<IReadOnlyList<ProductView>>;
