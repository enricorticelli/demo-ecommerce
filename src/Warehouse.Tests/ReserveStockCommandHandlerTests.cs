using Microsoft.Extensions.Logging;
using Moq;
using Shared.BuildingBlocks.Contracts.IntegrationEvents;
using Shared.BuildingBlocks.Contracts.IntegrationEvents.Order;
using Shared.BuildingBlocks.Contracts.IntegrationEvents.Warehouse;
using Shared.BuildingBlocks.Contracts.Messaging;
using Warehouse.Application.Abstractions.Idempotency;
using Warehouse.Application.Abstractions.Services;
using Warehouse.Application.Handlers;
using Warehouse.Application.Services;
using Xunit;

namespace Warehouse.Tests;

public sealed class ReserveStockCommandHandlerTests
{
    [Fact]
    public async Task Order_created_handler_should_publish_stock_reserved()
    {
        var orderCreated = new OrderCreatedV1(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "stripe_card",
            120m,
            "Pending",
            new IntegrationEventMetadata(Guid.NewGuid(), DateTimeOffset.UtcNow, "corr-1", "Order"));

        var expected = new StockReservationResult(orderCreated.OrderId, true, null);

        var reservationService = new Mock<IStockReservationService>();
        reservationService
            .Setup(x => x.ReserveAsync(orderCreated, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var deduplicationStore = new Mock<IWarehouseEventDeduplicationStore>();
        deduplicationStore
            .Setup(x => x.HasProcessedAsync(orderCreated.Metadata.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var publisher = new Mock<IDomainEventPublisher>();
        publisher
            .Setup(x => x.PublishAndFlushAsync(It.IsAny<IntegrationEventBase>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var logger = new Mock<ILogger<ReserveStockOnOrderCreatedHandler>>();
        var sut = new ReserveStockOnOrderCreatedHandler(reservationService.Object, deduplicationStore.Object, publisher.Object, logger.Object);

        await sut.Handle(orderCreated, CancellationToken.None);

        publisher.Verify(
            x => x.PublishAndFlushAsync(It.Is<StockReservedV1>(e => e.OrderId == orderCreated.OrderId), It.IsAny<CancellationToken>()),
            Times.Once);
        deduplicationStore.Verify(x => x.MarkProcessedAsync(orderCreated.Metadata.EventId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Order_created_handler_should_be_idempotent_for_duplicates()
    {
        var orderCreated = new OrderCreatedV1(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "stripe_card",
            120m,
            "Pending",
            new IntegrationEventMetadata(Guid.NewGuid(), DateTimeOffset.UtcNow, "corr-1", "Order"));

        var reservationService = new Mock<IStockReservationService>();
        var deduplicationStore = new Mock<IWarehouseEventDeduplicationStore>();
        deduplicationStore
            .Setup(x => x.HasProcessedAsync(orderCreated.Metadata.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var publisher = new Mock<IDomainEventPublisher>();

        var logger = new Mock<ILogger<ReserveStockOnOrderCreatedHandler>>();
        var sut = new ReserveStockOnOrderCreatedHandler(reservationService.Object, deduplicationStore.Object, publisher.Object, logger.Object);

        await sut.Handle(orderCreated, CancellationToken.None);

        reservationService.Verify(x => x.ReserveAsync(It.IsAny<OrderCreatedV1>(), It.IsAny<CancellationToken>()), Times.Never);
        publisher.Verify(x => x.PublishAndFlushAsync(It.IsAny<IntegrationEventBase>(), It.IsAny<CancellationToken>()), Times.Never);
        deduplicationStore.Verify(x => x.MarkProcessedAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
