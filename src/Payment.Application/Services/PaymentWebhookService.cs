using Payment.Application.Abstractions.Commands;
using Payment.Application.Abstractions.Idempotency;
using Payment.Application.Abstractions.Providers;
using Payment.Application.Abstractions.Repositories;
using Payment.Application.Abstractions.Services;

namespace Payment.Application.Services;

public sealed class PaymentWebhookService(
    IPaymentSessionRepository paymentSessionRepository,
    IPaymentWebhookDeduplicationStore webhookDeduplicationStore,
    IPaymentProviderRouter paymentProviderRouter,
    IPaymentCommandService paymentCommandService) : IPaymentWebhookService
{
    public async Task<PaymentWebhookProcessResult> ProcessAsync(
        string providerCode,
        string rawPayload,
        IReadOnlyDictionary<string, string> headers,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var verifier = paymentProviderRouter.ResolveWebhookVerifier(providerCode);
        if (!verifier.VerifySignature(rawPayload, headers))
        {
            return new PaymentWebhookProcessResult(PaymentWebhookProcessStatus.InvalidSignature, "Invalid webhook signature.");
        }

        var providerClient = paymentProviderRouter.ResolveByProviderCode(providerCode);
        PaymentWebhookNotification notification;

        try
        {
            notification = providerClient.ParseWebhook(rawPayload);
        }
        catch
        {
            return new PaymentWebhookProcessResult(PaymentWebhookProcessStatus.InvalidPayload, "Webhook payload is invalid.");
        }

        if (string.IsNullOrWhiteSpace(notification.ExternalEventId))
        {
            return new PaymentWebhookProcessResult(PaymentWebhookProcessStatus.InvalidPayload, "Missing external event id.");
        }

        if (await webhookDeduplicationStore.HasProcessedAsync(providerCode, notification.ExternalEventId, cancellationToken))
        {
            return new PaymentWebhookProcessResult(PaymentWebhookProcessStatus.Duplicate, "Webhook already processed.");
        }

        Domain.Entities.PaymentSession? session = null;
        if (notification.SessionId.HasValue && notification.SessionId.Value != Guid.Empty)
        {
            session = await paymentSessionRepository.GetBySessionIdAsync(notification.SessionId.Value, cancellationToken);
        }

        if (session is null && !string.IsNullOrWhiteSpace(notification.ExternalCheckoutId))
        {
            session = await paymentSessionRepository.GetByExternalCheckoutIdAsync(notification.ExternalCheckoutId, cancellationToken);
        }

        if (session is null)
        {
            await webhookDeduplicationStore.MarkProcessedAsync(providerCode, notification.ExternalEventId, cancellationToken);
            return new PaymentWebhookProcessResult(PaymentWebhookProcessStatus.SessionNotFound, "Payment session not found.");
        }

        var metadataChanged = session.RegisterProviderWebhook(
            providerCode,
            notification.ExternalCheckoutId,
            notification.ProviderStatus,
            notification.ExternalEventId,
            rawPayload);

        if (metadataChanged)
        {
            await paymentSessionRepository.SaveChangesAsync(cancellationToken);
        }

        if (notification.Decision == PaymentWebhookDecision.Authorized)
        {
            await paymentCommandService.AuthorizeAsync(session.SessionId, correlationId, notification.TransactionId, cancellationToken);
        }
        else if (notification.Decision == PaymentWebhookDecision.Rejected)
        {
            await paymentCommandService.RejectAsync(
                session.SessionId,
                notification.FailureReason,
                correlationId,
                cancellationToken);
        }

        await webhookDeduplicationStore.MarkProcessedAsync(providerCode, notification.ExternalEventId, cancellationToken);
        return new PaymentWebhookProcessResult(PaymentWebhookProcessStatus.Processed, "Webhook processed.");
    }
}
