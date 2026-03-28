using Microsoft.Extensions.Logging;
using Moq;
using Shared.BuildingBlocks.Contracts.IntegrationEvents;
using Shared.BuildingBlocks.Contracts.IntegrationEvents.Order;
using Shipping.Application.Abstractions.Commands;
using Shipping.Application.Abstractions.Idempotency;
using Shipping.Application.Commands;
using Shipping.Application.Handlers;
using Shipping.Application.Views;
using Xunit;

namespace Shipping.Tests;

public sealed class CreateShipmentOnOrderCompletedHandlerTests
{
    [Fact]
    public async Task HandleAsync_OrderCompleted_CreatesShipmentAndMarksProcessed()
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

        var commandService = new Mock<IShippingCommandService>();
        commandService
            .Setup(x => x.CreateAsync(It.IsAny<CreateShipmentCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ShipmentView(
                Guid.NewGuid(),
                integrationEvent.OrderId,
                integrationEvent.UserId,
                "TRK-LOCAL",
                "Created",
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow,
                null));

        var deduplicationStore = new Mock<IShippingEventDeduplicationStore>();
        deduplicationStore
            .Setup(x => x.HasProcessedAsync(integrationEvent.Metadata.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var logger = new Mock<ILogger<CreateShipmentOnOrderCompletedHandler>>();
        var sut = new CreateShipmentOnOrderCompletedHandler(commandService.Object, deduplicationStore.Object, logger.Object);

        await sut.Handle(integrationEvent, CancellationToken.None);

        commandService.Verify(
            x => x.CreateAsync(
                It.Is<CreateShipmentCommand>(c =>
                    c.OrderId == integrationEvent.OrderId
                    && c.UserId == integrationEvent.UserId),
                It.IsAny<CancellationToken>()),
            Times.Once);
        deduplicationStore.Verify(x => x.MarkProcessedAsync(integrationEvent.Metadata.EventId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_EventDuplicate_SkipsProcessing()
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

        var commandService = new Mock<IShippingCommandService>();
        var deduplicationStore = new Mock<IShippingEventDeduplicationStore>();
        deduplicationStore
            .Setup(x => x.HasProcessedAsync(integrationEvent.Metadata.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var logger = new Mock<ILogger<CreateShipmentOnOrderCompletedHandler>>();
        var sut = new CreateShipmentOnOrderCompletedHandler(commandService.Object, deduplicationStore.Object, logger.Object);

        await sut.Handle(integrationEvent, CancellationToken.None);

        commandService.Verify(x => x.CreateAsync(It.IsAny<CreateShipmentCommand>(), It.IsAny<CancellationToken>()), Times.Never);
        deduplicationStore.Verify(x => x.MarkProcessedAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}

