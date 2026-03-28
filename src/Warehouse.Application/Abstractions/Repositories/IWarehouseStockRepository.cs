using WarehouseStockItemEntity = Warehouse.Domain.Entities.WarehouseStockItem;

namespace Warehouse.Application.Abstractions.Repositories;

public interface IWarehouseStockRepository
{
    Task<WarehouseStockItemEntity?> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken);
    Task<IReadOnlyList<WarehouseStockItemEntity>> GetByProductIdsAsync(IReadOnlyCollection<Guid> productIds, CancellationToken cancellationToken);
    void Add(WarehouseStockItemEntity stockItem);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
