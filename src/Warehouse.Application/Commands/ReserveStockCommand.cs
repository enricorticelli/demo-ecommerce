namespace Warehouse.Application.Commands;

public sealed record ReserveStockCommand(Guid OrderId, IReadOnlyList<ReserveStockItemCommand> Items);

public sealed record ReserveStockItemCommand(Guid ProductId, string Sku, string Name, int Quantity, decimal UnitPrice);
