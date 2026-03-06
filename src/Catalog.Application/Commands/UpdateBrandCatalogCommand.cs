using Shared.BuildingBlocks.Cqrs;

namespace Catalog.Application;

public sealed record UpdateBrandCatalogCommand(Guid BrandId, UpdateBrandCommand Brand) : ICommand<BrandView?>;
