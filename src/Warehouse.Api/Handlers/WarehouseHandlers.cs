using Marten;
using Shared.BuildingBlocks.Contracts;
using Warehouse.Api.Domain;
using Wolverine;

namespace Warehouse.Api.Handlers;

public class WarehouseHandlers
{
    public static async Task Handle(OrderPlacedV1 message, IDocumentSession session, IMessageBus bus, CancellationToken cancellationToken)
    {
        var productIds = message.Items.Select(i => i.ProductId).ToArray();
        var docs = await session.Query<StockDocument>()
            .Where(x => productIds.Contains(x.Id))
            .ToListAsync(cancellationToken);
        var byId = docs.ToDictionary(x => x.Id, x => x);

        foreach (var item in message.Items)
        {
            if (!byId.TryGetValue(item.ProductId, out var stock) || stock.AvailableQuantity < item.Quantity)
            {
                await bus.PublishAsync(new StockRejectedV1(message.OrderId, $"Insufficient stock for {item.Sku}"));
                return;
            }
        }

        foreach (var item in message.Items)
        {
            byId[item.ProductId].AvailableQuantity -= item.Quantity;
        }

        await session.SaveChangesAsync(cancellationToken);
        await bus.PublishAsync(new StockReservedV1(message.OrderId));
    }
}
