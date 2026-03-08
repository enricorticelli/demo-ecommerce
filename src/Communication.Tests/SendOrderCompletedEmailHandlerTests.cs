using Communication.Application.Abstractions.Email;
using Communication.Application.Abstractions.Idempotency;
using Communication.Application.Handlers;
using Microsoft.Extensions.Logging;
using Moq;
using Shared.BuildingBlocks.Contracts.IntegrationEvents;
using Shared.BuildingBlocks.Contracts.IntegrationEvents.Order;
using Xunit;

namespace Communication.Tests;

public sealed class SendOrderCompletedEmailHandlerTests
{
    [Fact]
    public async Task Should_send_email_and_mark_event_processed_once()
    {
        var integrationEvent = new OrderCompletedV1(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "TRK-1",
            "TX-1",
            "customer@example.com",
            149.90m,
            new IntegrationEventMetadata(Guid.NewGuid(), DateTimeOffset.UtcNow, "corr-1", "Order"));

        var emailSender = new Mock<IEmailSender>();
        var deduplicationStore = new Mock<ICommunicationEventDeduplicationStore>();
        deduplicationStore.Setup(x => x.HasProcessedAsync(integrationEvent.Metadata.EventId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var logger = new Mock<ILogger<SendOrderCompletedEmailHandler>>();
        var sut = new SendOrderCompletedEmailHandler(emailSender.Object, deduplicationStore.Object, logger.Object);

        await sut.Handle(integrationEvent, CancellationToken.None);

        emailSender.Verify(x => x.SendAsync(integrationEvent.CustomerEmail, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        deduplicationStore.Verify(x => x.MarkProcessedAsync(integrationEvent.Metadata.EventId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Should_skip_duplicate_event()
    {
        var integrationEvent = new OrderCompletedV1(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "TRK-1",
            "TX-1",
            "customer@example.com",
            149.90m,
            new IntegrationEventMetadata(Guid.NewGuid(), DateTimeOffset.UtcNow, "corr-1", "Order"));

        var emailSender = new Mock<IEmailSender>();
        var deduplicationStore = new Mock<ICommunicationEventDeduplicationStore>();
        deduplicationStore.Setup(x => x.HasProcessedAsync(integrationEvent.Metadata.EventId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var logger = new Mock<ILogger<SendOrderCompletedEmailHandler>>();
        var sut = new SendOrderCompletedEmailHandler(emailSender.Object, deduplicationStore.Object, logger.Object);

        await sut.Handle(integrationEvent, CancellationToken.None);

        emailSender.Verify(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        deduplicationStore.Verify(x => x.MarkProcessedAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
