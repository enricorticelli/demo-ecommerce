using Shared.BuildingBlocks.Cqrs;

namespace Catalog.Application;

public sealed record DeleteBrandCatalogCommand(Guid BrandId) : ICommand<bool>;
