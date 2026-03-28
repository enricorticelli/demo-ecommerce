using Payment.Api.Contracts;
using Payment.Api.Contracts.Responses;
using Payment.Api.Mappers;
using Payment.Application.Abstractions.Queries;
using Payment.Application.Abstractions.Services;
using Payment.Application.Services;
using Payment.Application.Views;
using Shared.BuildingBlocks.Api;
using Shared.BuildingBlocks.Api.Correlation;
using Shared.BuildingBlocks.Exceptions;
using System.Text;

namespace Payment.Api.Endpoints;

public static class PaymentEndpoints
{
    public static RouteGroupBuilder MapPaymentEndpoints(this IEndpointRouteBuilder app)
    {
        var storeGroup = app.MapGroup(PaymentRoutes.StoreBase)
            .WithTags("Payment");

        storeGroup.MapGet("/sessions/orders/{orderId:guid}", GetPaymentSessionByOrderId)
            .WithName("StoreGetPaymentSessionByOrderId");
        storeGroup.MapGet("/sessions/{sessionId:guid}", GetPaymentSessionById)
            .WithName("StoreGetPaymentSessionById");

        var webhookGroup = app.MapGroup(PaymentRoutes.StoreBase)
            .WithTags("Payment");

        webhookGroup.MapPost("/webhooks/stripe", ProcessStripeWebhook)
            .AllowAnonymous()
            .WithName("StoreProcessStripeWebhook");
        webhookGroup.MapPost("/webhooks/paypal", ProcessPayPalWebhook)
            .AllowAnonymous()
            .WithName("StoreProcessPayPalWebhook");
        webhookGroup.MapPost("/webhooks/satispay", ProcessSatispayWebhook)
            .AllowAnonymous()
            .WithName("StoreProcessSatispayWebhook");
        return storeGroup;
    }

    private static async Task<IResult> GetPaymentSessionByOrderId(
        HttpContext context,
        Guid orderId,
        IPaymentQueryService queryService,
        CancellationToken cancellationToken)
    {
        var actorUserId = context.ResolveActorId();
        var session = await queryService.GetOrCreateByOrderIdAsync(orderId, cancellationToken);
        if (session is null)
        {
            return Results.NotFound();
        }

        EnsurePaymentOwnership(session, actorUserId);

        return Results.Ok(PaymentMapper.ToResponse(session));
    }

    private static async Task<IResult> GetPaymentSessionById(
        HttpContext context,
        Guid sessionId,
        IPaymentQueryService queryService,
        CancellationToken cancellationToken)
    {
        var actorUserId = context.ResolveActorId();
        var session = await queryService.GetBySessionIdAsync(sessionId, cancellationToken);
        if (session is null)
        {
            return Results.NotFound();
        }

        EnsurePaymentOwnership(session, actorUserId);

        return Results.Ok(PaymentMapper.ToResponse(session));
    }

    private static Task<IResult> ProcessStripeWebhook(
        HttpContext httpContext,
        IPaymentWebhookService webhookService,
        CancellationToken cancellationToken)
    {
        return ProcessWebhook("stripe", httpContext, webhookService, cancellationToken);
    }

    private static Task<IResult> ProcessPayPalWebhook(
        HttpContext httpContext,
        IPaymentWebhookService webhookService,
        CancellationToken cancellationToken)
    {
        return ProcessWebhook("paypal", httpContext, webhookService, cancellationToken);
    }

    private static Task<IResult> ProcessSatispayWebhook(
        HttpContext httpContext,
        IPaymentWebhookService webhookService,
        CancellationToken cancellationToken)
    {
        return ProcessWebhook("satispay", httpContext, webhookService, cancellationToken);
    }

    private static async Task<IResult> ProcessWebhook(
        string providerCode,
        HttpContext httpContext,
        IPaymentWebhookService webhookService,
        CancellationToken cancellationToken)
    {
        string rawPayload;
        using (var reader = new StreamReader(httpContext.Request.Body, Encoding.UTF8))
        {
            rawPayload = await reader.ReadToEndAsync(cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(rawPayload))
        {
            return Results.BadRequest(new PaymentWebhookResponse(providerCode, "InvalidPayload", "Empty payload."));
        }

        var headers = httpContext.Request.Headers
            .ToDictionary(
                x => x.Key,
                x => x.Value.ToString(),
                StringComparer.OrdinalIgnoreCase);

        var correlationId = CorrelationIdResolver.Resolve(httpContext);
        var result = await webhookService.ProcessAsync(
            providerCode,
            rawPayload,
            headers,
            correlationId,
            cancellationToken);

        if (result.Status == PaymentWebhookProcessStatus.InvalidSignature)
        {
            return Results.Unauthorized();
        }

        if (result.Status == PaymentWebhookProcessStatus.InvalidPayload)
        {
            return Results.BadRequest(new PaymentWebhookResponse(providerCode, result.Status.ToString(), result.Message));
        }

        return Results.Ok(new PaymentWebhookResponse(providerCode, result.Status.ToString(), result.Message));
    }

    private static void EnsurePaymentOwnership(PaymentSessionView session, Guid authenticatedUserId)
    {
        if (session.UserId != authenticatedUserId)
        {
            throw new ForbiddenAppException("The payment session does not belong to the authenticated user.");
        }
    }
}
