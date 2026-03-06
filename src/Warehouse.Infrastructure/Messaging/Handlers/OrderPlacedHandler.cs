using Shared.BuildingBlocks.Contracts;
using Warehouse.Application;
using Wolverine;

namespace Warehouse.Infrastructure;

public sealed class OrderPlacedHandler
{
    public static async Task Handle(OrderPlacedV1 message, IWarehouseService warehouseService, IMessageBus bus, CancellationToken cancellationToken)
    {
        var result = await warehouseService.ReserveStockAsync(
            new StockReserveRequestedV1(message.OrderId, message.Items),
            cancellationToken);
        if (!result.Reserved)
        {
            await bus.PublishAsync(new StockRejectedV1(message.OrderId, result.Reason ?? "Stock not available"));
            return;
        }

        await bus.PublishAsync(new StockReservedV1(message.OrderId));
    }
}
