using Marten;
using Shared.BuildingBlocks.Contracts;
using Warehouse.Application;
using Warehouse.Domain;

namespace Warehouse.Infrastructure;

public sealed class WarehouseService(IDocumentSession documentSession) : IWarehouseService
{
    public async Task UpsertStockAsync(UpsertStockItem model, CancellationToken cancellationToken)
    {
        var existing = await documentSession.LoadAsync<StockAggregate>(model.ProductId, cancellationToken);
        if (existing is null)
        {
            documentSession.Store(new StockAggregate
            {
                Id = model.ProductId,
                Sku = model.Sku,
                AvailableQuantity = model.AvailableQuantity
            });
        }
        else
        {
            existing.AvailableQuantity = model.AvailableQuantity;
            documentSession.Store(existing);
        }

        await documentSession.SaveChangesAsync(cancellationToken);
    }

    public async Task<StockReservationResult> ReserveStockAsync(StockReserveRequestedV1 request, CancellationToken cancellationToken)
    {
        var productIds = request.Items.Select(i => i.ProductId).ToArray();
        var docs = await documentSession.Query<StockAggregate>()
            .Where(x => productIds.Contains(x.Id))
            .ToListAsync(cancellationToken);
        var byId = docs.ToDictionary(x => x.Id, x => x);

        var missing = request.Items.Where(i => !byId.ContainsKey(i.ProductId) || byId[i.ProductId].AvailableQuantity < i.Quantity).ToList();
        if (missing.Count > 0)
        {
            return new StockReservationResult(request.OrderId, false, "Stock not available");
        }

        foreach (var item in request.Items)
        {
            byId[item.ProductId].AvailableQuantity -= item.Quantity;
            documentSession.Store(byId[item.ProductId]);
        }

        await documentSession.SaveChangesAsync(cancellationToken);
        return new StockReservationResult(request.OrderId, true);
    }
}
