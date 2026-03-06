using Shared.BuildingBlocks.Cqrs;

namespace Catalog.Application;

public sealed record UpdateCategoryCatalogCommand(Guid CategoryId, UpdateCategoryCommand Category) : ICommand<CategoryView?>;
