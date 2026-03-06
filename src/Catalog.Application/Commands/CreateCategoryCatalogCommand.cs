using Shared.BuildingBlocks.Cqrs;

namespace Catalog.Application;

public sealed record CreateCategoryCatalogCommand(CreateCategoryCommand Category) : ICommand<CategoryView>;
