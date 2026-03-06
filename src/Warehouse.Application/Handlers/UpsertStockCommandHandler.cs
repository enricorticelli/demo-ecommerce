using Shared.BuildingBlocks.Cqrs;

namespace Warehouse.Application;

public sealed class UpsertStockCommandHandler(IWarehouseService warehouseService)
    : ICommandHandler<UpsertStockCommand, Unit>
{
    public async Task<Unit> HandleAsync(UpsertStockCommand command, CancellationToken cancellationToken)
    {
        await warehouseService.UpsertStockAsync(command.Item, cancellationToken);
        return Unit.Value;
    }
}
