using Shared.BuildingBlocks.Cqrs;

namespace Catalog.Application;

public sealed record CreateBrandCatalogCommand(CreateBrandCommand Brand) : ICommand<BrandView>;
