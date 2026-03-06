using Shared.BuildingBlocks.Cqrs;

namespace Catalog.Application;

public sealed record DeleteCategoryCatalogCommand(Guid CategoryId) : ICommand<bool>;
