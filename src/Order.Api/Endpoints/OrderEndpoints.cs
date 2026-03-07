using Order.Api.Contracts;
using Shared.BuildingBlocks.Api;

namespace Order.Api.Endpoints;

public static class OrderEndpoints
{
    public static RouteGroupBuilder MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(OrderRoutes.Base)
            .WithTags("Order")
            .AddEndpointFilter<CqrsExceptionEndpointFilter>();

        group.MapPost("/", CreateOrder)
            .WithName("CreateOrder");
        group.MapGet("/", ListOrders)
            .WithName("ListOrders");
        group.MapGet("/{orderId:guid}", GetOrder)
            .WithName("GetOrder");
        group.MapPost("/{orderId:guid}/manual-complete", ManualCompleteOrder)
            .WithName("ManualCompleteOrder");
        group.MapPost("/{orderId:guid}/manual-cancel", ManualCancelOrder)
            .WithName("ManualCancelOrder");
        return group;
    }

}
