using Microsoft.AspNetCore.Http.HttpResults;
using Shared.BuildingBlocks.Contracts;
using Wolverine;

namespace Payment.Api.Endpoints;

public static class PaymentEndpoints
{
    public static RouteGroupBuilder MapPaymentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/payments")
            .WithTags("Payment");

        group.MapPost("/authorize", AuthorizePayment)
            .WithName("AuthorizePayment");

        return group;
    }

    private static async Task<Ok<object>> AuthorizePayment(PaymentAuthorizeRequestedV1 request, IMessageBus bus)
    {
        if (request.Amount <= 0 || request.Amount > 10000)
        {
            await bus.PublishAsync(new PaymentFailedV1(request.OrderId, "Payment declined"));
            return TypedResults.Ok((object)new { request.OrderId, Authorized = false });
        }

        var transactionId = $"TX-{Guid.NewGuid():N}";
        await bus.PublishAsync(new PaymentAuthorizedV1(request.OrderId, transactionId));
        return TypedResults.Ok((object)new { request.OrderId, Authorized = true, TransactionId = transactionId });
    }
}
