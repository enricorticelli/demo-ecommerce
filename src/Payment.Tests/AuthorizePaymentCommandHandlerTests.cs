using Microsoft.Extensions.Logging;
using Moq;
using Payment.Application.Abstractions.Idempotency;
using Payment.Application.Abstractions.Repositories;
using Payment.Application.Handlers;
using Shared.BuildingBlocks.Contracts.IntegrationEvents;
using Shared.BuildingBlocks.Contracts.IntegrationEvents.Order;
using Xunit;

namespace Payment.Tests;

public sealed class AuthorizePaymentCommandHandlerTests
{
    [Fact]
    public async Task HandleOrderCreated_NewEvent_CreatesPaymentSessionWithoutPublishingResult()
    {
        var orderCreated = new OrderCreatedV1(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "paypal",
            120m,
            "Pending",
            new IntegrationEventMetadata(Guid.NewGuid(), DateTimeOffset.UtcNow, "corr-1", "Order"));

        var deduplicationStore = new Mock<IPaymentEventDeduplicationStore>();
        deduplicationStore
            .Setup(x => x.HasProcessedAsync(orderCreated.Metadata.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var paymentSessionRepository = new Mock<IPaymentSessionRepository>();
        paymentSessionRepository
            .Setup(x => x.GetByOrderIdAsync(orderCreated.OrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Domain.Entities.PaymentSession?)null);
        paymentSessionRepository
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var logger = new Mock<ILogger<AuthorizePaymentOnOrderCreatedHandler>>();
        var sut = new AuthorizePaymentOnOrderCreatedHandler(
            paymentSessionRepository.Object,
            deduplicationStore.Object,
            logger.Object);

        await sut.Handle(orderCreated, CancellationToken.None);

        paymentSessionRepository.Verify(x => x.Add(It.IsAny<Domain.Entities.PaymentSession>()), Times.Once);
        paymentSessionRepository.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        deduplicationStore.Verify(x => x.MarkProcessedAsync(orderCreated.Metadata.EventId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleOrderCreated_DuplicateEvent_SkipsProcessing()
    {
        var orderCreated = new OrderCreatedV1(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "stripe_card",
            120m,
            "Pending",
            new IntegrationEventMetadata(Guid.NewGuid(), DateTimeOffset.UtcNow, "corr-1", "Order"));

        var deduplicationStore = new Mock<IPaymentEventDeduplicationStore>();
        var paymentSessionRepository = new Mock<IPaymentSessionRepository>();
        deduplicationStore
            .Setup(x => x.HasProcessedAsync(orderCreated.Metadata.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var logger = new Mock<ILogger<AuthorizePaymentOnOrderCreatedHandler>>();
        var sut = new AuthorizePaymentOnOrderCreatedHandler(
            paymentSessionRepository.Object,
            deduplicationStore.Object,
            logger.Object);

        await sut.Handle(orderCreated, CancellationToken.None);

        paymentSessionRepository.Verify(x => x.GetByOrderIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        paymentSessionRepository.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        deduplicationStore.Verify(x => x.MarkProcessedAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}

