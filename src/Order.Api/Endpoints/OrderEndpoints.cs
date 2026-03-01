using System.Text.Json;
using System.Net.Http.Json;
using FluentValidation;
using Marten;
using Microsoft.AspNetCore.Http.HttpResults;
using Order.Api.Application;
using Order.Api.Domain;
using Shared.BuildingBlocks.Contracts;
using Wolverine;

namespace Order.Api.Endpoints;

public static class OrderEndpoints
{
    public static RouteGroupBuilder MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/orders")
            .WithTags("Order");

        group.MapPost("/", CreateOrder)
            .WithName("CreateOrder");

        group.MapGet("/{orderId:guid}", GetOrder)
            .WithName("GetOrder");

        return group;
    }

    private static async Task<IResult> CreateOrder(
        CreateOrderCommand command,
        IValidator<CreateOrderCommand> validator,
        IHttpClientFactory httpClientFactory,
        IDocumentSession session,
        IMessageBus bus,
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
                detail: "Invalid create order command",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: new Dictionary<string, object?> { ["errors"] = errors });
        }

        var cartClient = httpClientFactory.CreateClient("cart");
        var response = await cartClient.GetAsync($"/v1/carts/{command.CartId}", cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return TypedResults.NotFound();
        }

        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var cart = JsonSerializer.Deserialize<CartSnapshot>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        if (cart is null || cart.Items.Count == 0)
        {
            return TypedResults.Problem(
                title: "Invalid cart",
                detail: "Cart is empty or unreadable",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var orderId = Guid.NewGuid();
        session.Events.StartStream<OrderAggregate>(orderId, new OrderPlacedDomain(orderId, command.CartId, command.UserId, cart.Items, cart.TotalAmount));
        await session.SaveChangesAsync(cancellationToken);

        await bus.PublishAsync(new OrderPlacedV1(orderId, command.UserId, cart.Items, cart.TotalAmount));

        var warehouseClient = httpClientFactory.CreateClient("warehouse");
        var reserveResponse = await warehouseClient.PostAsJsonAsync(
            "/v1/stock/reserve",
            new StockReserveRequestedV1(orderId, cart.Items),
            cancellationToken);
        reserveResponse.EnsureSuccessStatusCode();

        var reservePayload = await reserveResponse.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
        var isReserved = reservePayload.TryGetProperty("reserved", out var reservedElement) && reservedElement.GetBoolean();
        if (!isReserved)
        {
            var reason = reservePayload.TryGetProperty("reason", out var reasonElement) ? reasonElement.GetString() ?? "Stock not available" : "Stock not available";
            var failStream = await session.Events.FetchForWriting<OrderAggregate>(orderId, cancellationToken);
            failStream.AppendOne(new OrderFailedDomain(orderId, reason));
            await session.SaveChangesAsync(cancellationToken);
            await bus.PublishAsync(new OrderFailedV1(orderId, reason));
            return TypedResults.Accepted($"/v1/orders/{orderId}", new { orderId, status = OrderStatus.Failed.ToString() });
        }

        var reserveStream = await session.Events.FetchForWriting<OrderAggregate>(orderId, cancellationToken);
        reserveStream.AppendOne(new OrderStockReservedDomain(orderId));
        await session.SaveChangesAsync(cancellationToken);

        var paymentClient = httpClientFactory.CreateClient("payment");
        var paymentResponse = await paymentClient.PostAsJsonAsync(
            "/v1/payments/authorize",
            new PaymentAuthorizeRequestedV1(orderId, command.UserId, cart.TotalAmount),
            cancellationToken);
        paymentResponse.EnsureSuccessStatusCode();

        var paymentPayload = await paymentResponse.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
        var isAuthorized = paymentPayload.TryGetProperty("authorized", out var authElement) && authElement.GetBoolean();
        if (!isAuthorized)
        {
            var failStream = await session.Events.FetchForWriting<OrderAggregate>(orderId, cancellationToken);
            failStream.AppendOne(new OrderFailedDomain(orderId, "Payment declined"));
            await session.SaveChangesAsync(cancellationToken);
            await bus.PublishAsync(new OrderFailedV1(orderId, "Payment declined"));
            return TypedResults.Accepted($"/v1/orders/{orderId}", new { orderId, status = OrderStatus.Failed.ToString() });
        }

        var transactionId = paymentPayload.TryGetProperty("transactionId", out var txElement) ? txElement.GetString() ?? string.Empty : string.Empty;
        var paymentStream = await session.Events.FetchForWriting<OrderAggregate>(orderId, cancellationToken);
        paymentStream.AppendOne(new OrderPaymentAuthorizedDomain(orderId, transactionId));
        await session.SaveChangesAsync(cancellationToken);

        var shippingClient = httpClientFactory.CreateClient("shipping");
        var shippingResponse = await shippingClient.PostAsJsonAsync(
            "/v1/shipments",
            new ShippingCreateRequestedV1(orderId, command.UserId, cart.Items),
            cancellationToken);
        shippingResponse.EnsureSuccessStatusCode();
        var shippingPayload = await shippingResponse.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
        var trackingCode = shippingPayload.TryGetProperty("trackingCode", out var trackingElement) ? trackingElement.GetString() ?? string.Empty : string.Empty;

        var completeStream = await session.Events.FetchForWriting<OrderAggregate>(orderId, cancellationToken);
        completeStream.AppendOne(new OrderCompletedDomain(orderId, trackingCode, transactionId));
        await session.SaveChangesAsync(cancellationToken);
        await bus.PublishAsync(new OrderCompletedV1(orderId, trackingCode, transactionId));

        return TypedResults.Accepted($"/v1/orders/{orderId}", new { orderId, status = OrderStatus.Completed.ToString() });
    }

    private static async Task<Results<Ok<object>, NotFound>> GetOrder(Guid orderId, IQuerySession session, CancellationToken cancellationToken)
    {
        var order = await session.Events.AggregateStreamAsync<OrderAggregate>(orderId, token: cancellationToken);
        if (order is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok((object)new
        {
            order.Id,
            order.CartId,
            order.UserId,
            Status = order.Status.ToString(),
            order.TotalAmount,
            order.Items,
            order.TrackingCode,
            order.TransactionId,
            order.FailureReason
        });
    }
}
