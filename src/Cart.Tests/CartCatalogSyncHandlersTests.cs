using Cart.Application.Abstractions.Idempotency;
using Cart.Application.Abstractions.Repositories;
using Cart.Application.Handlers;
using Cart.Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;
using Shared.BuildingBlocks.Contracts.IntegrationEvents;
using Shared.BuildingBlocks.Contracts.IntegrationEvents.Catalog;
using Xunit;

namespace Cart.Tests;

public sealed class CartCatalogSyncHandlersTests
{
    [Fact]
    public async Task HandleProductUpdated_OpenCartsContainProduct_SyncsSku()
    {
        var productId = Guid.NewGuid();
        var cart = Cart.Domain.Entities.Cart.Create(Guid.NewGuid(), Guid.NewGuid());
        cart.AddItem(CartItem.Create(productId, "SKU-1", "Item 1", 1, 10m));
        cart.AddItem(CartItem.Create(Guid.NewGuid(), "SKU-2", "Item 2", 1, 20m));

        var integrationEvent = new ProductUpdatedV1(
            productId,
            "SKU-NEW",
            Guid.NewGuid(),
            Guid.NewGuid(),
            [],
            new IntegrationEventMetadata(Guid.NewGuid(), DateTimeOffset.UtcNow, "corr-1", "Catalog"));

        var repository = new Mock<ICartRepository>();
        repository
            .Setup(x => x.ListByProductIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([cart]);

        var deduplicationStore = new Mock<ICartEventDeduplicationStore>();
        deduplicationStore
            .Setup(x => x.HasProcessedAsync(integrationEvent.Metadata.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var logger = new Mock<ILogger<SyncCartOnProductUpdatedHandler>>();
        var sut = new SyncCartOnProductUpdatedHandler(repository.Object, deduplicationStore.Object, logger.Object);

        await sut.Handle(integrationEvent, CancellationToken.None);

        var updated = Assert.Single(cart.Items.Where(x => x.ProductId == productId));
        Assert.Equal("SKU-NEW", updated.Sku);
        Assert.Equal(2, cart.Items.Count);
        repository.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        deduplicationStore.Verify(x => x.MarkProcessedAsync(integrationEvent.Metadata.EventId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleProductDeleted_DuplicateEvent_SkipsProcessing()
    {
        var integrationEvent = new ProductDeletedV1(
            Guid.NewGuid(),
            new IntegrationEventMetadata(Guid.NewGuid(), DateTimeOffset.UtcNow, "corr-1", "Catalog"));

        var repository = new Mock<ICartRepository>();
        var deduplicationStore = new Mock<ICartEventDeduplicationStore>();
        deduplicationStore
            .Setup(x => x.HasProcessedAsync(integrationEvent.Metadata.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var logger = new Mock<ILogger<SyncCartOnProductDeletedHandler>>();
        var sut = new SyncCartOnProductDeletedHandler(repository.Object, deduplicationStore.Object, logger.Object);

        await sut.Handle(integrationEvent, CancellationToken.None);

        repository.Verify(x => x.ListByProductIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        repository.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        deduplicationStore.Verify(x => x.MarkProcessedAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}

