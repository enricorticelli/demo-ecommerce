using Payment.Application.Abstractions.Commands;
using Payment.Application.Abstractions.Services;
using Shared.BuildingBlocks.Contracts.IntegrationEvents;
using Shared.BuildingBlocks.Contracts.IntegrationEvents.Payment;
using Shared.BuildingBlocks.Contracts.Messaging;

namespace Payment.Application.Services;

public sealed class PaymentCommandService(
    IPaymentSessionService paymentSessionService,
    IDomainEventPublisher eventPublisher) : IPaymentCommandService
{
    public async Task<PaymentSessionUpdateResult?> AuthorizeAsync(
        Guid sessionId,
        string correlationId,
        string? transactionId,
        CancellationToken cancellationToken)
    {
        var update = await paymentSessionService.AuthorizeAsync(sessionId, transactionId, cancellationToken);
        if (update is null)
        {
            return null;
        }

        if (update.StatusChanged)
        {
            var integrationEvent = new PaymentAuthorizedV1(
                update.Session.OrderId,
                update.Session.TransactionId ?? string.Empty,
                CreateMetadata(correlationId));

            await eventPublisher.PublishAndFlushAsync(integrationEvent, cancellationToken);
        }

        return update;
    }

    public async Task<PaymentSessionUpdateResult?> RejectAsync(
        Guid sessionId,
        string? reason,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var update = await paymentSessionService.RejectAsync(sessionId, reason, cancellationToken);
        if (update is null)
        {
            return null;
        }

        if (update.StatusChanged)
        {
            var integrationEvent = new PaymentRejectedV1(
                update.Session.OrderId,
                update.Session.FailureReason ?? "Payment rejected.",
                CreateMetadata(correlationId));

            await eventPublisher.PublishAndFlushAsync(integrationEvent, cancellationToken);
        }

        return update;
    }

    private static IntegrationEventMetadata CreateMetadata(string correlationId)
    {
        return IntegrationEventMetadataFactory.Create(correlationId, "Payment");
    }
}
