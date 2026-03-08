using Communication.Application.Abstractions.Email;
using Communication.Application.Abstractions.Idempotency;
using Communication.Domain.Entities;
using Microsoft.Extensions.Logging;
using Shared.BuildingBlocks.Contracts.IntegrationEvents.Order;
using Shared.BuildingBlocks.Messaging;

namespace Communication.Application.Handlers;

public sealed class SendOrderCompletedEmailHandler(
    IEmailSender emailSender,
    ICommunicationEventDeduplicationStore deduplicationStore,
    ILogger<SendOrderCompletedEmailHandler> logger)
    : IntegrationEventHandlerBase<OrderCompletedV1>(deduplicationStore, logger)
{
    public Task Handle(OrderCompletedV1 integrationEvent, CancellationToken cancellationToken)
    {
        var message = CommunicationEmailMessage.ForOrderCompleted(
            integrationEvent.OrderId,
            integrationEvent.TotalAmount,
            integrationEvent.CustomerEmail);

        return HandleDeduplicatedAsync(
            integrationEvent,
            ct => emailSender.SendAsync(
                message.Recipient,
                message.Subject,
                message.Body,
                ct),
            cancellationToken);
    }
}
