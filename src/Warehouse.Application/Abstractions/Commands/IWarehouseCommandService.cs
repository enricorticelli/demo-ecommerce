using Warehouse.Application.Commands;
using Warehouse.Application.Views;

namespace Warehouse.Application.Abstractions.Commands;

public interface IWarehouseCommandService
{
    Task<WarehouseStockView> UpsertStockAsync(UpsertStockCommand command, CancellationToken cancellationToken);
    Task<WarehouseReserveView> ReserveStockAsync(ReserveStockCommand command, CancellationToken cancellationToken);
}
