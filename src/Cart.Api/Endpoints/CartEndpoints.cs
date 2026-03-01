using Cart.Api.Application;
using Cart.Api.Domain;
using FluentValidation;
using Marten;
using Microsoft.AspNetCore.Http.HttpResults;
using Shared.BuildingBlocks.Contracts;

namespace Cart.Api.Endpoints;

public static class CartEndpoints
{
    public static RouteGroupBuilder MapCartEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/carts")
            .WithTags("Cart")
            ;

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
        IValidator<AddCartItemCommand> validator,
        IDocumentSession session,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            var errors = validation.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            return TypedResults.Problem(
                title: "Validation error",
                detail: "Invalid add item command",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: new Dictionary<string, object?> { ["errors"] = errors });
        }

        var stream = await session.Events.FetchForWriting<CartAggregate>(cartId, cancellationToken);
        if (!stream.Events.Any())
        {
            stream.AppendOne(new CartCreated(cartId, command.UserId));
        }

        stream.AppendOne(new CartItemAdded(cartId, command.ProductId, command.Sku, command.Name, command.Quantity, command.UnitPrice));
        await session.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok((object)new { cartId, message = "Item added" });
    }

    private static async Task<Ok<object>> RemoveItem(Guid cartId, Guid productId, IDocumentSession session, CancellationToken cancellationToken)
    {
        var stream = await session.Events.FetchForWriting<CartAggregate>(cartId, cancellationToken);
        stream.AppendOne(new CartItemRemoved(cartId, productId));
        await session.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok((object)new { cartId, productId, message = "Item removed" });
    }

    private static async Task<Results<Ok<object>, NotFound>> GetCart(Guid cartId, IQuerySession session, CancellationToken cancellationToken)
    {
        var cart = await session.Events.AggregateStreamAsync<CartAggregate>(cartId, token: cancellationToken);
        if (cart is null)
        {
            return TypedResults.NotFound();
        }

        var result = new
        {
            CartId = cart.Id,
            cart.UserId,
            Items = cart.Lines.Values.Select(line => new OrderItemDto(line.ProductId, line.Sku, line.Name, line.Quantity, line.UnitPrice)).ToList(),
            cart.TotalAmount
        };

        return TypedResults.Ok((object)result);
    }

    private static async Task<Results<Ok<CartCheckedOutV1>, NotFound>> CheckoutCart(Guid cartId, IQuerySession session, CancellationToken cancellationToken)
    {
        var cart = await session.Events.AggregateStreamAsync<CartAggregate>(cartId, token: cancellationToken);
        if (cart is null || cart.Lines.Count == 0)
        {
            return TypedResults.NotFound();
        }

        var evt = new CartCheckedOutV1(
            cartId,
            Guid.NewGuid(),
            cart.UserId,
            cart.Lines.Values.Select(line => new OrderItemDto(line.ProductId, line.Sku, line.Name, line.Quantity, line.UnitPrice)).ToList(),
            cart.TotalAmount);

        return TypedResults.Ok(evt);
    }
}
