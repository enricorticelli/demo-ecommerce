using Shared.BuildingBlocks.Cqrs;

namespace Catalog.Application;

public sealed record GetCategoriesQuery : IQuery<IReadOnlyList<CategoryView>>;
