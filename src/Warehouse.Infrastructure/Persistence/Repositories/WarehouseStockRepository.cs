using Microsoft.EntityFrameworkCore;
using Warehouse.Application.Abstractions.Repositories;
using WarehouseStockItemEntity = Warehouse.Domain.Entities.WarehouseStockItem;

namespace Warehouse.Infrastructure.Persistence.Repositories;

public sealed class WarehouseStockRepository(WarehouseDbContext dbContext) : IWarehouseStockRepository
{
    public Task<WarehouseStockItemEntity?> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken)
    {
        return dbContext.WarehouseStockItems
            .FirstOrDefaultAsync(x => x.ProductId == productId, cancellationToken);
    }

    public async Task<IReadOnlyList<WarehouseStockItemEntity>> GetByProductIdsAsync(IReadOnlyCollection<Guid> productIds, CancellationToken cancellationToken)
    {
        return await dbContext.WarehouseStockItems
            .Where(x => productIds.Contains(x.ProductId))
            .ToArrayAsync(cancellationToken);
    }

    public void Add(WarehouseStockItemEntity stockItem)
    {
        dbContext.WarehouseStockItems.Add(stockItem);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
