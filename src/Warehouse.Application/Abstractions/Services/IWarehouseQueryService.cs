using Warehouse.Application.Views;

namespace Warehouse.Application.Abstractions.Services;

public interface IWarehouseQueryService
{
    Task<IReadOnlyList<WarehouseStockView>> GetByProductIdsAsync(
        IReadOnlyCollection<Guid> productIds,
        int? lowStockThreshold,
        CancellationToken cancellationToken);
}
