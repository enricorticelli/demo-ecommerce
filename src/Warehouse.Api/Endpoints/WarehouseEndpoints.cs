using Microsoft.AspNetCore.Http.HttpResults;
using Shared.BuildingBlocks.Cqrs;
using Shared.BuildingBlocks.Api;
using Shared.BuildingBlocks.Contracts;
using Warehouse.Api.Contracts;
using Warehouse.Application;

namespace Warehouse.Api.Endpoints;

public static class WarehouseEndpoints
{
    public static RouteGroupBuilder MapWarehouseEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(WarehouseRoutes.Base)
            .WithTags("Warehouse")
            .AddEndpointFilter<CqrsExceptionEndpointFilter>();

        group.MapPost("/", UpsertStock)
            .WithName("UpsertStock");

        group.MapPost("/reserve", ReserveStock)
            .WithName("ReserveStock");

        return group;
    }

    private static async Task<Ok<object>> ReserveStock(StockReserveRequestedV1 request, ICommandDispatcher commandDispatcher, CancellationToken cancellationToken)
    {
        var result = await commandDispatcher.ExecuteAsync(new ReserveStockCommand(request), cancellationToken);
        return TypedResults.Ok((object)new { result.OrderId, result.Reserved, result.Reason });
    }

    private static async Task<Ok<object>> UpsertStock(UpsertStockRequest request, ICommandDispatcher commandDispatcher, CancellationToken cancellationToken)
    {
        var command = new UpsertStockCommand(new UpsertStockItem(request.ProductId, request.Sku, request.AvailableQuantity));
        await commandDispatcher.ExecuteAsync(command, cancellationToken);
        return TypedResults.Ok((object)new
        {
            request.ProductId,
            request.Sku,
            request.AvailableQuantity
        });
    }
}
