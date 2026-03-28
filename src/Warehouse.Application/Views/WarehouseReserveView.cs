namespace Warehouse.Application.Views;

public sealed record WarehouseReserveView(Guid OrderId, bool Reserved, string? Reason);
