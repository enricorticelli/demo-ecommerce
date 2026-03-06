using Microsoft.AspNetCore.Http.HttpResults;
using Payment.Application;
using Payment.Api.Contracts;
using Shared.BuildingBlocks.Api;
using Shared.BuildingBlocks.Contracts;
using Shared.BuildingBlocks.Cqrs;

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
        group.MapPost("/sessions/{sessionId:guid}/authorize", AuthorizePaymentSession)
            .WithName("AuthorizePaymentSession");
        group.MapPost("/sessions/{sessionId:guid}/reject", RejectPaymentSession)
            .WithName("RejectPaymentSession");

        return group;
    }

    private static async Task<Ok<object>> AuthorizePayment(
        PaymentAuthorizeRequestedV1 request,
        ICommandDispatcher commandDispatcher,
        CancellationToken cancellationToken)
    {
        var result = await commandDispatcher.ExecuteAsync(new AuthorizePaymentCommand(request), cancellationToken);
        return TypedResults.Ok((object)new { result.OrderId, result.Authorized, result.TransactionId });
    }

    private static async Task<Results<Ok<PaymentSessionView>, NotFound>> GetPaymentSessionByOrderId(
        Guid orderId,
        IQueryDispatcher queryDispatcher,
        CancellationToken cancellationToken)
    {
        var session = await queryDispatcher.ExecuteAsync(new GetPaymentSessionByOrderIdQuery(orderId), cancellationToken);
        return session is null ? TypedResults.NotFound() : TypedResults.Ok(session);
    }

    private static async Task<Ok<IReadOnlyList<PaymentSessionView>>> ListPaymentSessions(
        IQueryDispatcher queryDispatcher,
        int? limit,
        CancellationToken cancellationToken)
    {
        var safeLimit = Math.Clamp(limit ?? 50, 1, 200);
        var sessions = await queryDispatcher.ExecuteAsync(new GetPaymentSessionsQuery(safeLimit), cancellationToken);
        return TypedResults.Ok(sessions);
    }

    private static async Task<Results<Ok<PaymentSessionView>, NotFound>> GetPaymentSessionById(
        Guid sessionId,
        IQueryDispatcher queryDispatcher,
        CancellationToken cancellationToken)
    {
        var session = await queryDispatcher.ExecuteAsync(new GetPaymentSessionByIdQuery(sessionId), cancellationToken);
        return session is null ? TypedResults.NotFound() : TypedResults.Ok(session);
    }

    private static async Task<Results<Ok<object>, NotFound>> AuthorizePaymentSession(
        Guid sessionId,
        ICommandDispatcher commandDispatcher,
        CancellationToken cancellationToken)
    {
        var found = await commandDispatcher.ExecuteAsync(new AuthorizePaymentSessionCommand(sessionId), cancellationToken);
        if (!found)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok((object)new { sessionId, status = "Authorized" });
    }

    private static async Task<Results<Ok<object>, NotFound>> RejectPaymentSession(
        Guid sessionId,
        RejectPaymentSessionRequest request,
        ICommandDispatcher commandDispatcher,
        CancellationToken cancellationToken)
    {
        var rejected = await commandDispatcher.ExecuteAsync(
            new RejectPaymentSessionCommand(sessionId, request.Reason ?? "Payment declined"),
            cancellationToken);

        return rejected
            ? TypedResults.Ok((object)new { sessionId, status = "Rejected" })
            : TypedResults.NotFound();
    }
}
