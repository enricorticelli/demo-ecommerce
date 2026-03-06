using Shared.BuildingBlocks.Cqrs;

namespace Warehouse.Application;

public sealed class ReserveStockCommandHandler(IWarehouseService warehouseService)
    : ICommandHandler<ReserveStockCommand, StockReservationResult>
{
    public Task<StockReservationResult> HandleAsync(ReserveStockCommand command, CancellationToken cancellationToken)
    {
        return warehouseService.ReserveStockAsync(command.Request, cancellationToken);
    }
}
