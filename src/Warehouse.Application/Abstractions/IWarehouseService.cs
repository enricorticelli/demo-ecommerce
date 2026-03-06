using Shared.BuildingBlocks.Contracts;

namespace Warehouse.Application;

public interface IWarehouseService
{
    Task<StockReservationResult> ReserveStockAsync(StockReserveRequestedV1 request, CancellationToken cancellationToken);
    Task UpsertStockAsync(UpsertStockItem model, CancellationToken cancellationToken);
}
