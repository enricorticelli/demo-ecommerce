using Shared.BuildingBlocks.Cqrs;

namespace Catalog.Application;

public sealed record GetNewArrivalsQuery : IQuery<IReadOnlyList<ProductView>>;
