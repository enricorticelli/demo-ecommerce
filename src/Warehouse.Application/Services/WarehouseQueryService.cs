using Warehouse.Application.Abstractions.Repositories;
using Warehouse.Application.Abstractions.Services;
using Warehouse.Application.Views;

namespace Warehouse.Application.Services;

public sealed class WarehouseQueryService(IWarehouseStockRepository stockRepository) : IWarehouseQueryService
{
    public async Task<IReadOnlyList<WarehouseStockView>> GetByProductIdsAsync(
        IReadOnlyCollection<Guid> productIds,
        int? lowStockThreshold,
        CancellationToken cancellationToken)
    {
        if (productIds.Count == 0)
        {
            return Array.Empty<WarehouseStockView>();
        }

        var stockItems = await stockRepository.GetByProductIdsAsync(productIds, cancellationToken);

        var views = stockItems
            .Select(x => new WarehouseStockView(x.ProductId, x.Sku, x.AvailableQuantity));

        if (lowStockThreshold.HasValue)
        {
            views = views.Where(x => x.AvailableQuantity <= lowStockThreshold.Value);
        }

        return views
            .OrderBy(x => x.AvailableQuantity)
            .ThenBy(x => x.Sku)
            .ToArray();
    }
}
