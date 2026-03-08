using Moq;
using Order.Application.Abstractions.Idempotency;
using Order.Application.Abstractions.Repositories;
using Order.Application.Handlers;
using Order.Domain.ValueObjects;
using Shared.BuildingBlocks.Contracts.IntegrationEvents;
using Shared.BuildingBlocks.Contracts.IntegrationEvents.Payment;
using Shared.BuildingBlocks.Contracts.IntegrationEvents.Order;
using Shared.BuildingBlocks.Contracts.IntegrationEvents.Warehouse;
using Shared.BuildingBlocks.Contracts.Messaging;
using Xunit;

namespace Order.Tests;

public sealed class OrderIntegrationHandlersTests
{
    [Fact]
    public async Task Payment_authorized_handler_should_update_order_and_mark_event_processed()
    {
        var order = BuildOrder();
        var integrationEvent = new PaymentAuthorizedV1(order.Id, "TX-1", new IntegrationEventMetadata(Guid.NewGuid(), DateTimeOffset.UtcNow, "corr-1", "Payment"));

        var repository = new Mock<IOrderRepository>();
        repository.Setup(x => x.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);

        var deduplication = new Mock<IOrderEventDeduplicationStore>();
        deduplication.Setup(x => x.HasProcessedAsync(integrationEvent.Metadata.EventId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var publisher = new Mock<IDomainEventPublisher>();
        publisher
            .Setup(x => x.PublishAndFlushAsync(It.IsAny<IntegrationEventBase>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = new HandlePaymentAuthorizedOnOrderHandler(repository.Object, deduplication.Object, publisher.Object);

        await sut.HandleAsync(integrationEvent, CancellationToken.None);

        Assert.True(order.IsPaymentAuthorized);
        Assert.Equal("TX-1", order.TransactionId);
        repository.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        deduplication.Verify(x => x.MarkProcessedAsync(integrationEvent.Metadata.EventId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Stock_reserved_handler_should_complete_order_when_payment_was_already_authorized()
    {
        var order = BuildOrder();
        order.ApplyPaymentAuthorized("TX-1");
        var integrationEvent = new StockReservedV1(order.Id, new IntegrationEventMetadata(Guid.NewGuid(), DateTimeOffset.UtcNow, "corr-1", "Warehouse"));

        var repository = new Mock<IOrderRepository>();
        repository.Setup(x => x.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);

        var deduplication = new Mock<IOrderEventDeduplicationStore>();
        deduplication.Setup(x => x.HasProcessedAsync(integrationEvent.Metadata.EventId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var publisher = new Mock<IDomainEventPublisher>();
        publisher
            .Setup(x => x.PublishAndFlushAsync(It.IsAny<IntegrationEventBase>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = new HandleStockReservedOnOrderHandler(repository.Object, deduplication.Object, publisher.Object);

        await sut.HandleAsync(integrationEvent, CancellationToken.None);

        Assert.Equal(Domain.Entities.OrderStatus.Completed, order.Status);
        Assert.True(order.IsStockReserved);
    }

    [Fact]
    public async Task Rejection_handler_should_be_idempotent_for_duplicates()
    {
        var order = BuildOrder();
        var integrationEvent = new PaymentRejectedV1(order.Id, "Rejected", new IntegrationEventMetadata(Guid.NewGuid(), DateTimeOffset.UtcNow, "corr-1", "Payment"));

        var repository = new Mock<IOrderRepository>();
        var deduplication = new Mock<IOrderEventDeduplicationStore>();
        var publisher = new Mock<IDomainEventPublisher>();
        deduplication.Setup(x => x.HasProcessedAsync(integrationEvent.Metadata.EventId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var sut = new HandlePaymentRejectedOnOrderHandler(repository.Object, deduplication.Object, publisher.Object);

        await sut.HandleAsync(integrationEvent, CancellationToken.None);

        repository.Verify(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        repository.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        deduplication.Verify(x => x.MarkProcessedAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        publisher.Verify(x => x.PublishAndFlushAsync(It.IsAny<IntegrationEventBase>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Stock_rejected_handler_should_cancel_order()
    {
        var order = BuildOrder();
        var integrationEvent = new StockRejectedV1(order.Id, "Out of stock", new IntegrationEventMetadata(Guid.NewGuid(), DateTimeOffset.UtcNow, "corr-1", "Warehouse"));

        var repository = new Mock<IOrderRepository>();
        repository.Setup(x => x.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);

        var deduplication = new Mock<IOrderEventDeduplicationStore>();
        deduplication.Setup(x => x.HasProcessedAsync(integrationEvent.Metadata.EventId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var publisher = new Mock<IDomainEventPublisher>();
        publisher
            .Setup(x => x.PublishAndFlushAsync(It.IsAny<IntegrationEventBase>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = new HandleStockRejectedOnOrderHandler(repository.Object, deduplication.Object, publisher.Object);

        await sut.HandleAsync(integrationEvent, CancellationToken.None);

        Assert.Equal(Domain.Entities.OrderStatus.Cancelled, order.Status);
        Assert.Equal("Out of stock", order.FailureReason);
        publisher.Verify(
            x => x.PublishAndFlushAsync(It.Is<OrderCancelledV1>(e => e.OrderId == order.Id && e.Reason == "Out of stock"), It.IsAny<CancellationToken>()),
            Times.Once);
        repository.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private static Domain.Entities.Order BuildOrder()
    {
        return Domain.Entities.Order.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "authenticated",
            "card",
            Guid.NewGuid(),
            null,
            OrderCustomer.Create("Mario", "Rossi", "mario@example.com", "+39000000000"),
            OrderAddress.Create("Street 1", "Rome", "00100", "IT"),
            OrderAddress.Create("Street 1", "Rome", "00100", "IT"),
            [OrderItem.Create(Guid.NewGuid(), "SKU-1", "Item 1", 2, 10m)],
            20m);
    }
}
