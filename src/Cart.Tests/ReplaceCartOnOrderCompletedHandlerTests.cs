using Cart.Application.Abstractions.Idempotency;
using Cart.Application.Abstractions.Repositories;
using Cart.Application.Handlers;
using Microsoft.Extensions.Logging;
using Moq;
using Shared.BuildingBlocks.Contracts.IntegrationEvents;
using Shared.BuildingBlocks.Contracts.IntegrationEvents.Order;
using Xunit;

namespace Cart.Tests;

public sealed class ReplaceCartOnOrderCompletedHandlerTests
{
    [Fact]
    public async Task HandleAsync_OrderCompleted_ReplacesCart()
    {
        var cart = Domain.Entities.Cart.Create(Guid.NewGuid(), Guid.NewGuid());
        var integrationEvent = new OrderCompletedV1(
            Guid.NewGuid(),
            cart.Id,
            cart.UserId,
            "TRK-1",
            "TX-1",
            "customer@example.com",
            149.90m,
            new IntegrationEventMetadata(Guid.NewGuid(), DateTimeOffset.UtcNow, "corr-1", "Order"));

        var repository = new Mock<ICartRepository>();
        repository
            .Setup(x => x.GetByIdAsync(cart.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

        var deduplicationStore = new Mock<ICartEventDeduplicationStore>();
        deduplicationStore
            .Setup(x => x.HasProcessedAsync(integrationEvent.Metadata.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var logger = new Mock<ILogger<ReplaceCartOnOrderCompletedHandler>>();
        var sut = new ReplaceCartOnOrderCompletedHandler(repository.Object, deduplicationStore.Object, logger.Object);

        await sut.Handle(integrationEvent, CancellationToken.None);

        repository.Verify(x => x.Remove(cart), Times.Once);
        repository.Verify(x => x.Add(It.Is<Cart.Domain.Entities.Cart>(c => c.UserId == integrationEvent.UserId && c.Id != integrationEvent.CartId)), Times.Once);
        repository.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
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

        var repository = new Mock<ICartRepository>();
        var deduplicationStore = new Mock<ICartEventDeduplicationStore>();
        deduplicationStore
            .Setup(x => x.HasProcessedAsync(integrationEvent.Metadata.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var logger = new Mock<ILogger<ReplaceCartOnOrderCompletedHandler>>();
        var sut = new ReplaceCartOnOrderCompletedHandler(repository.Object, deduplicationStore.Object, logger.Object);

        await sut.Handle(integrationEvent, CancellationToken.None);

        repository.Verify(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        repository.Verify(x => x.Remove(It.IsAny<Cart.Domain.Entities.Cart>()), Times.Never);
        repository.Verify(x => x.Add(It.IsAny<Cart.Domain.Entities.Cart>()), Times.Never);
        repository.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        deduplicationStore.Verify(x => x.MarkProcessedAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}

