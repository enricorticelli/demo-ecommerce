using Microsoft.Extensions.Logging;
using Moq;
using Payment.Application.Abstractions.Idempotency;
using Payment.Application.Abstractions.Repositories;
using Payment.Application.Abstractions.Services;
using Payment.Application.Handlers;
using Payment.Application.Services;
using Shared.BuildingBlocks.Contracts.IntegrationEvents;
using Shared.BuildingBlocks.Contracts.IntegrationEvents.Order;
using Shared.BuildingBlocks.Contracts.IntegrationEvents.Payment;
using Shared.BuildingBlocks.Contracts.Messaging;
using Xunit;

namespace Payment.Tests;

public sealed class AuthorizePaymentCommandHandlerTests
{
    [Fact]
    public async Task Order_created_handler_should_publish_payment_authorized()
    {
        var orderCreated = new OrderCreatedV1(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            120m,
            "Pending",
            new IntegrationEventMetadata(Guid.NewGuid(), DateTimeOffset.UtcNow, "corr-1", "Order"));

        var expected = new PaymentAuthorizationResult(orderCreated.OrderId, true, "TX-1", null);

        var authorizationService = new Mock<IPaymentAuthorizationService>();
        authorizationService
            .Setup(x => x.AuthorizeAsync(orderCreated, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var deduplicationStore = new Mock<IPaymentEventDeduplicationStore>();
        deduplicationStore
            .Setup(x => x.HasProcessedAsync(orderCreated.Metadata.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var paymentSessionRepository = new Mock<IPaymentSessionRepository>();
        paymentSessionRepository
            .Setup(x => x.GetByOrderIdAsync(orderCreated.OrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Payment.Domain.Entities.PaymentSession?)null);
        paymentSessionRepository
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var publisher = new Mock<IDomainEventPublisher>();
        publisher
            .Setup(x => x.PublishAndFlushAsync(It.IsAny<IntegrationEventBase>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var logger = new Mock<ILogger<AuthorizePaymentOnOrderCreatedHandler>>();
        var sut = new AuthorizePaymentOnOrderCreatedHandler(
            authorizationService.Object,
            paymentSessionRepository.Object,
            deduplicationStore.Object,
            publisher.Object,
            logger.Object);

        await sut.Handle(orderCreated, CancellationToken.None);

        publisher.Verify(
            x => x.PublishAndFlushAsync(It.Is<PaymentAuthorizedV1>(e => e.OrderId == orderCreated.OrderId && e.TransactionId == "TX-1"), It.IsAny<CancellationToken>()),
            Times.Once);
        paymentSessionRepository.Verify(x => x.Add(It.IsAny<Payment.Domain.Entities.PaymentSession>()), Times.Once);
        paymentSessionRepository.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        deduplicationStore.Verify(x => x.MarkProcessedAsync(orderCreated.Metadata.EventId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Order_created_handler_should_be_idempotent_for_duplicates()
    {
        var orderCreated = new OrderCreatedV1(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            120m,
            "Pending",
            new IntegrationEventMetadata(Guid.NewGuid(), DateTimeOffset.UtcNow, "corr-1", "Order"));

        var authorizationService = new Mock<IPaymentAuthorizationService>();
        var deduplicationStore = new Mock<IPaymentEventDeduplicationStore>();
        var paymentSessionRepository = new Mock<IPaymentSessionRepository>();
        deduplicationStore
            .Setup(x => x.HasProcessedAsync(orderCreated.Metadata.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var publisher = new Mock<IDomainEventPublisher>();

        var logger = new Mock<ILogger<AuthorizePaymentOnOrderCreatedHandler>>();
        var sut = new AuthorizePaymentOnOrderCreatedHandler(
            authorizationService.Object,
            paymentSessionRepository.Object,
            deduplicationStore.Object,
            publisher.Object,
            logger.Object);

        await sut.Handle(orderCreated, CancellationToken.None);

        authorizationService.Verify(x => x.AuthorizeAsync(It.IsAny<OrderCreatedV1>(), It.IsAny<CancellationToken>()), Times.Never);
        paymentSessionRepository.Verify(x => x.GetByOrderIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        publisher.Verify(x => x.PublishAndFlushAsync(It.IsAny<IntegrationEventBase>(), It.IsAny<CancellationToken>()), Times.Never);
        deduplicationStore.Verify(x => x.MarkProcessedAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
