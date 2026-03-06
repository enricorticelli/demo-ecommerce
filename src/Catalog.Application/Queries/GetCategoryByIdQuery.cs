using Shared.BuildingBlocks.Cqrs;

namespace Catalog.Application;

public sealed record GetCategoryByIdQuery(Guid CategoryId) : IQuery<CategoryView?>;
