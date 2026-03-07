using Cart.Api.Contracts;
using Shared.BuildingBlocks.Api;

namespace Cart.Api.Endpoints;

public static class CartEndpoints
{
    public static RouteGroupBuilder MapCartEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(CartRoutes.Base)
            .WithTags("Cart")
            .AddEndpointFilter<CqrsExceptionEndpointFilter>();

        group.MapPost("/{cartId:guid}/items", AddItem)
            .WithName("AddCartItem");
        group.MapDelete("/{cartId:guid}/items/{productId:guid}", RemoveItem)
            .WithName("RemoveCartItem");
        group.MapGet("/{cartId:guid}", GetCart)
            .WithName("GetCart");
        group.MapPost("/{cartId:guid}/checkout", CheckoutCart)
            .WithName("CheckoutCart");
        return group;
    }

    //TODO
}
