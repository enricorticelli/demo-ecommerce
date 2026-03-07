using Payment.Api.Contracts;
using Shared.BuildingBlocks.Api;

namespace Payment.Api.Endpoints;

public static class PaymentEndpoints
{
    public static RouteGroupBuilder MapPaymentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(PaymentRoutes.Base)
            .WithTags("Payment")
            .AddEndpointFilter<CqrsExceptionEndpointFilter>();

        group.MapPost("/authorize", AuthorizePayment)
            .WithName("AuthorizePayment");
        group.MapGet("/sessions", ListPaymentSessions)
            .WithName("ListPaymentSessions");
        group.MapGet("/sessions/orders/{orderId:guid}", GetPaymentSessionByOrderId)
            .WithName("GetPaymentSessionByOrderId");
        group.MapGet("/sessions/{sessionId:guid}", GetPaymentSessionById)
            .WithName("GetPaymentSessionById");
        group.MapGet("/hosted/{paymentMethod}", RenderHostedPaymentPage)
            .WithName("RenderHostedPaymentPage");
        group.MapPost("/sessions/{sessionId:guid}/authorize", AuthorizePaymentSession)
            .WithName("AuthorizePaymentSession");
        group.MapPost("/sessions/{sessionId:guid}/reject", RejectPaymentSession)
            .WithName("RejectPaymentSession");
        return group;
    }

}
