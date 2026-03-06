using Cart.Application;
using Cart.Api.Contracts;
using Microsoft.AspNetCore.Http.HttpResults;
using Shared.BuildingBlocks.Api;
using Shared.BuildingBlocks.Contracts;
using Shared.BuildingBlocks.Cqrs;

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

    private static async Task<IResult> AddItem(
        Guid cartId,
        AddCartItemCommand command,
        ICommandDispatcher commandDispatcher,
        CancellationToken cancellationToken)
    {
        await commandDispatcher.ExecuteAsync(new AddCartItemToCartCommand(cartId, command), cancellationToken);
        return TypedResults.Ok((object)new { cartId, message = "Item added" });
    }

    private static async Task<Ok<object>> RemoveItem(Guid cartId, Guid productId, ICommandDispatcher commandDispatcher, CancellationToken cancellationToken)
    {
        await commandDispatcher.ExecuteAsync(new RemoveCartItemFromCartCommand(cartId, productId), cancellationToken);
        return TypedResults.Ok((object)new { cartId, productId, message = "Item removed" });
    }

    private static async Task<Results<Ok<object>, NotFound>> GetCart(Guid cartId, IQueryDispatcher queryDispatcher, CancellationToken cancellationToken)
    {
        var cart = await queryDispatcher.ExecuteAsync(new GetCartByIdQuery(cartId), cancellationToken);
        if (cart is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok((object)cart);
    }

    private static async Task<Results<Ok<CartCheckedOutV1>, NotFound>> CheckoutCart(Guid cartId, ICommandDispatcher commandDispatcher, CancellationToken cancellationToken)
    {
        var checkout = await commandDispatcher.ExecuteAsync(new CheckoutCartCommand(cartId), cancellationToken);
        return checkout is null ? TypedResults.NotFound() : TypedResults.Ok(checkout);
    }
}
