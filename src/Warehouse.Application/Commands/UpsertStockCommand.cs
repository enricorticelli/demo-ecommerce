using Shared.BuildingBlocks.Cqrs;

namespace Warehouse.Application;

public sealed record UpsertStockCommand(UpsertStockItem Item) : ICommand<Unit>;
