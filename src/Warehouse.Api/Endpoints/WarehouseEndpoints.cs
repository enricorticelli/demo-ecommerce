using Warehouse.Api.Contracts;
using Warehouse.Api.Contracts.Requests;
using Warehouse.Api.Contracts.Responses;
using Warehouse.Api.Mappers;
using Warehouse.Application.Abstractions.Commands;
using Warehouse.Application.Abstractions.Services;
using Shared.BuildingBlocks.Api.Errors;

namespace Warehouse.Api.Endpoints;

public static class WarehouseEndpoints
{
    public static RouteGroupBuilder MapWarehouseEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(WarehouseRoutes.BackofficeBase)
            .WithTags("Warehouse");

        group.MapPost("/query", QueryStock)
            .WithName("BackofficeQueryStock");

        group.MapPost("/", UpsertStock)
            .WithName("BackofficeUpsertStock");
        return group;
    }

    private static async Task<IResult> QueryStock(
        GetStockByProductsRequest request,
        IWarehouseQueryService service,
        CancellationToken cancellationToken)
    {
        try
        {
            var productIds = request.ProductIds
                .Where(x => x != Guid.Empty)
                .Distinct()
                .ToArray();

            var lowStockThreshold = request.LowStockThreshold.HasValue
                ? (int?)Math.Max(0, request.LowStockThreshold.Value)
                : null;

            var result = await service.GetByProductIdsAsync(productIds, lowStockThreshold, cancellationToken);
            return Results.Ok(result.ToResponse());
        }
        catch (Exception exception)
        {
            return ExceptionHttpResultMapper.Map(exception);
        }
    }

    private static async Task<IResult> UpsertStock(
        UpsertStockRequest request,
        IWarehouseCommandService service,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await service.UpsertStockAsync(request.ToCommand(), cancellationToken);
            return Results.Ok(result.ToResponse());
        }
        catch (Exception exception)
        {
            return ExceptionHttpResultMapper.Map(exception);
        }
    }

}
