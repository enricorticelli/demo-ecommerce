using Payment.Api.Contracts;
using Payment.Api.Contracts.Responses;
using Payment.Api.Mappers;
using Payment.Application.Abstractions.Queries;
using Payment.Application.Abstractions.Services;
using Payment.Application.Services;
using Shared.BuildingBlocks.Api.Correlation;
using System.Text;

namespace Payment.Api.Endpoints;

public static class PaymentEndpoints
{
    public static RouteGroupBuilder MapPaymentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(PaymentRoutes.StoreBase)
            .WithTags("Payment");

        group.MapGet("/sessions/orders/{orderId:guid}", GetPaymentSessionByOrderId)
            .WithName("StoreGetPaymentSessionByOrderId");
        group.MapGet("/sessions/{sessionId:guid}", GetPaymentSessionById)
            .WithName("StoreGetPaymentSessionById");
        group.MapPost("/webhooks/stripe", ProcessStripeWebhook)
            .WithName("StoreProcessStripeWebhook");
        group.MapPost("/webhooks/paypal", ProcessPayPalWebhook)
            .WithName("StoreProcessPayPalWebhook");
        group.MapPost("/webhooks/satispay", ProcessSatispayWebhook)
            .WithName("StoreProcessSatispayWebhook");
        return group;
    }

    private static async Task<IResult> GetPaymentSessionByOrderId(
        Guid orderId,
        IPaymentQueryService queryService,
        CancellationToken cancellationToken)
    {
        var session = await queryService.GetOrCreateByOrderIdAsync(orderId, cancellationToken);
        if (session is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(PaymentMapper.ToResponse(session));
    }

    private static async Task<IResult> GetPaymentSessionById(
        Guid sessionId,
        IPaymentQueryService queryService,
        CancellationToken cancellationToken)
    {
        var session = await queryService.GetBySessionIdAsync(sessionId, cancellationToken);
        if (session is null)
        {
            return Results.NotFound();
        }

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
}
