using Cart.Api.Contracts;
using Cart.Api.Contracts.Requests;
using Cart.Api.Contracts.Responses;
using Cart.Api.Mappers;
using Cart.Application.Abstractions.Commands;
using Cart.Application.Abstractions.Queries;
using Cart.Application.Commands;
using Cart.Application.Views;
using Shared.BuildingBlocks.Api;
using Shared.BuildingBlocks.Api.Errors;
using Shared.BuildingBlocks.Exceptions;

namespace Cart.Api.Endpoints;

public static class CartEndpoints
{
    public static RouteGroupBuilder MapCartEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(CartRoutes.StoreBase)
            .WithTags("Cart");

        group.MapPost("/{cartId:guid}/items", AddItem)
            .WithName("StoreAddCartItem");
        group.MapDelete("/{cartId:guid}/items/{productId:guid}", RemoveItem)
            .WithName("StoreRemoveCartItem");
        group.MapGet("/{cartId:guid}", GetCart)
            .WithName("StoreGetCart");
        return group;
    }

    private static async Task<IResult> AddItem(
        HttpContext context,
        Guid cartId,
        AddCartItemRequest request,
        ICartQueryService queryService,
        ICartCommandService service,
        CancellationToken cancellationToken)
    {
        try
        {
            var actorUserId = context.ResolveActorId();

            try
            {
                var existingCart = await queryService.GetByIdAsync(cartId, cancellationToken);
                EnsureCartOwnership(existingCart, actorUserId);
            }
            catch (NotFoundAppException)
            {
                // A missing cart can be created by AddItemAsync for the resolved actor identity.
            }

            await service.AddItemAsync(request.ToCommand(cartId, actorUserId), cancellationToken);
            return Results.Ok(new AddCartItemResponse(cartId, "Item added."));
        }
        catch (Exception exception)
        {
            return ExceptionHttpResultMapper.Map(exception);
        }
    }

    private static async Task<IResult> RemoveItem(
        HttpContext context,
        Guid cartId,
        Guid productId,
        ICartQueryService queryService,
        ICartCommandService service,
        CancellationToken cancellationToken)
    {
        try
        {
            var actorUserId = context.ResolveActorId();
            var cart = await queryService.GetByIdAsync(cartId, cancellationToken);
            EnsureCartOwnership(cart, actorUserId);

            await service.RemoveItemAsync(new RemoveCartItemCommand(cartId, productId), cancellationToken);
            return Results.Ok(new RemoveCartItemResponse(cartId, productId, "Item removed."));
        }
        catch (Exception exception)
        {
            return ExceptionHttpResultMapper.Map(exception);
        }
    }

    private static async Task<IResult> GetCart(
        HttpContext context,
        Guid cartId,
        ICartQueryService service,
        CancellationToken cancellationToken)
    {
        try
        {
            var actorUserId = context.ResolveActorId();
            var cart = await service.GetByIdAsync(cartId, cancellationToken);
            EnsureCartOwnership(cart, actorUserId);
            return Results.Ok(cart.ToResponse());
        }
        catch (Exception exception)
        {
            return ExceptionHttpResultMapper.Map(exception);
        }
    }

    private static void EnsureCartOwnership(CartView cart, Guid authenticatedUserId)
    {
        if (cart.UserId != authenticatedUserId)
        {
            throw new ForbiddenAppException("The cart does not belong to the authenticated user.");
        }
    }

}
