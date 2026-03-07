using Shared.BuildingBlocks.Api;
using Warehouse.Api.Contracts;

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

}
