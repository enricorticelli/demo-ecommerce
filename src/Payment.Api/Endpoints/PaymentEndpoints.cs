using Payment.Api.Contracts;
using Payment.Api.Contracts.Requests;
using Payment.Api.Contracts.Responses;
using Payment.Api.Mappers;
using Payment.Application.Abstractions.Services;
using Payment.Application.Views;
using Shared.BuildingBlocks.Api.Correlation;
using Shared.BuildingBlocks.Contracts.IntegrationEvents;
using Shared.BuildingBlocks.Contracts.IntegrationEvents.Payment;
using Shared.BuildingBlocks.Contracts.Messaging;

namespace Payment.Api.Endpoints;

public static class PaymentEndpoints
{
    public static RouteGroupBuilder MapPaymentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(PaymentRoutes.Base)
            .WithTags("Payment");

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

    private static async Task<IResult> ListPaymentSessions(
        IPaymentSessionService paymentSessionService,
        CancellationToken cancellationToken)
    {
        var sessions = await paymentSessionService.ListAsync(cancellationToken);

        return Results.Ok(sessions.Select(PaymentMapper.ToResponse));
    }

    private static async Task<IResult> GetPaymentSessionByOrderId(
        Guid orderId,
        IConfiguration configuration,
        IPaymentSessionService paymentSessionService,
        CancellationToken cancellationToken)
    {
        var redirectUrl = BuildHostedRedirectUrlTemplate(configuration, orderId);
        var session = await paymentSessionService.GetOrCreateByOrderIdAsync(orderId, redirectUrl, cancellationToken);

        return Results.Ok(PaymentMapper.ToResponse(session));
    }

    private static async Task<IResult> GetPaymentSessionById(
        Guid sessionId,
        IPaymentSessionService paymentSessionService,
        CancellationToken cancellationToken)
    {
        var session = await paymentSessionService.GetBySessionIdAsync(sessionId, cancellationToken);
        if (session is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(PaymentMapper.ToResponse(session));
    }

    private static IResult RenderHostedPaymentPage(string paymentMethod, Guid? sessionId, Guid? orderId)
    {
        var safeSessionId = sessionId?.ToString() ?? "n/a";
        var safeOrderId = orderId?.ToString() ?? "n/a";

        return Results.Content(
            $"Hosted endpoint placeholder for payment method '{paymentMethod}'. sessionId={safeSessionId}, orderId={safeOrderId}",
            "text/plain");
    }

    private static async Task<IResult> AuthorizePaymentSession(
        Guid sessionId,
        IPaymentSessionService paymentSessionService,
        IDomainEventPublisher eventPublisher,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var update = await paymentSessionService.AuthorizeAsync(sessionId, cancellationToken);
        if (update is null)
        {
            return Results.NotFound();
        }

        if (update.StatusChanged)
        {
            var correlationId = CorrelationIdResolver.Resolve(httpContext);
            var integrationEvent = new PaymentAuthorizedV1(
                update.Session.OrderId,
                update.Session.TransactionId ?? string.Empty,
                CreateMetadata(correlationId));

            await eventPublisher.PublishAndFlushAsync(integrationEvent, cancellationToken);
        }

        return Results.Ok(new PaymentSessionStatusResponse(sessionId, update.Session.Status));
    }

    private static async Task<IResult> RejectPaymentSession(
        Guid sessionId,
        RejectPaymentSessionRequest request,
        IPaymentSessionService paymentSessionService,
        IDomainEventPublisher eventPublisher,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var update = await paymentSessionService.RejectAsync(sessionId, request.Reason, cancellationToken);
        if (update is null)
        {
            return Results.NotFound();
        }

        if (update.StatusChanged)
        {
            var correlationId = CorrelationIdResolver.Resolve(httpContext);
            var integrationEvent = new PaymentRejectedV1(
                update.Session.OrderId,
                update.Session.FailureReason ?? "Payment rejected.",
                CreateMetadata(correlationId));

            await eventPublisher.PublishAndFlushAsync(integrationEvent, cancellationToken);
        }

        return Results.Ok(new PaymentSessionStatusResponse(sessionId, update.Session.Status));
    }

    private static string BuildHostedRedirectUrlTemplate(IConfiguration configuration, Guid orderId)
    {
        var baseUrl = configuration["Payment:HostedReturnBaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            baseUrl = "http://localhost:3000";
        }

        return $"{baseUrl.TrimEnd('/')}/payment/session/{{sessionId}}?orderId={orderId}";
    }

    private static IntegrationEventMetadata CreateMetadata(string correlationId)
    {
        return new IntegrationEventMetadata(Guid.NewGuid(), DateTimeOffset.UtcNow, correlationId, "Payment");
    }

}
