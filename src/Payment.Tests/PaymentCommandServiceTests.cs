using Moq;
using Payment.Application.Abstractions.Services;
using Payment.Application.Services;
using Payment.Application.Views;
using Shared.BuildingBlocks.Contracts.IntegrationEvents;
using Shared.BuildingBlocks.Contracts.IntegrationEvents.Payment;
using Shared.BuildingBlocks.Contracts.Messaging;
using Xunit;

namespace Payment.Tests;

public sealed class PaymentCommandServiceTests
{
    [Fact]
    public async Task Authorize_should_publish_payment_authorized_when_status_changes()
    {
        var sessionId = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        var sessionService = new Mock<IPaymentSessionService>();
        sessionService
            .Setup(x => x.AuthorizeAsync(sessionId, "TX-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentSessionUpdateResult(
                new PaymentSessionView(
                    sessionId,
                    orderId,
                    Guid.NewGuid(),
                    120m,
                    "stripe_card",
                    "stripe",
                    "chk_1",
                    "checkout.session.completed",
                    "Authorized",
                    "TX-123",
                    null,
                    DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow,
                    "http://localhost/payment/session/1"),
                true));

        var publisher = new Mock<IDomainEventPublisher>();
        publisher
            .Setup(x => x.PublishAndFlushAsync(It.IsAny<IntegrationEventBase>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = new PaymentCommandService(sessionService.Object, publisher.Object);
        var update = await sut.AuthorizeAsync(sessionId, "corr-123", "TX-123", CancellationToken.None);

        Assert.NotNull(update);
        publisher.Verify(
            x => x.PublishAndFlushAsync(
                It.Is<PaymentAuthorizedV1>(integrationEvent =>
                    integrationEvent.OrderId == orderId
                    && integrationEvent.TransactionId == "TX-123"
                    && integrationEvent.Metadata.CorrelationId == "corr-123"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Reject_should_not_publish_when_status_is_unchanged()
    {
        var sessionId = Guid.NewGuid();

        var sessionService = new Mock<IPaymentSessionService>();
        sessionService
            .Setup(x => x.RejectAsync(sessionId, "already rejected", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentSessionUpdateResult(
                new PaymentSessionView(
                    sessionId,
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    120m,
                    "stripe_card",
                    "stripe",
                    "chk_1",
                    "checkout.session.expired",
                    "Rejected",
                    null,
                    "already rejected",
                    DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow,
                    "http://localhost/payment/session/1"),
                false));

        var publisher = new Mock<IDomainEventPublisher>();
        var sut = new PaymentCommandService(sessionService.Object, publisher.Object);

        var update = await sut.RejectAsync(sessionId, "already rejected", "corr-456", CancellationToken.None);

        Assert.NotNull(update);
        publisher.Verify(
            x => x.PublishAndFlushAsync(It.IsAny<IntegrationEventBase>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
