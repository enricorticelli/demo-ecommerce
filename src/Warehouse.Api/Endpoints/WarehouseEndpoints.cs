using Marten;
using Microsoft.AspNetCore.Http.HttpResults;
using Shared.BuildingBlocks.Contracts;
using Warehouse.Api.Domain;

namespace Warehouse.Api.Endpoints;

public static class WarehouseEndpoints
{
    public static RouteGroupBuilder MapWarehouseEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/stock")
            .WithTags("Warehouse")
            ;

        group.MapPost("/reserve", ReserveStock)
            .WithName("ReserveStock");

        var internalGroup = app.MapGroup("/internal/seed")
            .WithTags("WarehouseInternal")
            ;

        internalGroup.MapPost("/stock", SeedStock)
            .WithName("SeedStock");

        return group;
    }

    private static async Task<Ok<object>> ReserveStock(StockReserveRequestedV1 request, IDocumentSession session, CancellationToken cancellationToken)
    {
        var productIds = request.Items.Select(i => i.ProductId).ToArray();
        var docs = await session.Query<StockDocument>()
            .Where(x => productIds.Contains(x.Id))
            .ToListAsync(cancellationToken);
        var byId = docs.ToDictionary(x => x.Id, x => x);

        var missing = request.Items.Where(i => !byId.ContainsKey(i.ProductId) || byId[i.ProductId].AvailableQuantity < i.Quantity).ToList();
        if (missing.Count > 0)
        {
            return TypedResults.Ok((object)new { request.OrderId, Reserved = false, Reason = "Stock not available" });
        }

        foreach (var item in request.Items)
        {
            byId[item.ProductId].AvailableQuantity -= item.Quantity;
        }

        await session.SaveChangesAsync(cancellationToken);
        return TypedResults.Ok((object)new { request.OrderId, Reserved = true });
    }

    private static async Task<Ok<object>> SeedStock(IDocumentSession session, CancellationToken cancellationToken)
    {
        var seed = new[]
        {
            new StockDocument { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), Sku = "SKU-KEYBOARD-001", AvailableQuantity = 100 },
            new StockDocument { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), Sku = "SKU-MOUSE-001", AvailableQuantity = 100 },
            new StockDocument { Id = Guid.Parse("33333333-3333-3333-3333-333333333333"), Sku = "SKU-HEADSET-001", AvailableQuantity = 100 }
        };

        foreach (var stock in seed)
        {
            session.Store(stock);
        }

        await session.SaveChangesAsync(cancellationToken);
        return TypedResults.Ok((object)new { Seeded = seed.Length });
    }
}
