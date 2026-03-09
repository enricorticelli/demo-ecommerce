using Moq;
using Shared.BuildingBlocks.Contracts.IntegrationEvents;
using Shared.BuildingBlocks.Contracts.IntegrationEvents.Order;
using Warehouse.Application.Abstractions.Repositories;
using Warehouse.Application.Services;
using Xunit;

namespace Warehouse.Tests;

public sealed class StockReservationServiceTests
{
    [Fact]
    public async Task ReserveAsync_NewOrder_PersistsReservation()
    {
        var orderCreated = new OrderCreatedV1(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "stripe_card",
            100m,
            "Pending",
            new IntegrationEventMetadata(Guid.NewGuid(), DateTimeOffset.UtcNow, "corr-1", "Order"));

        var repository = new Mock<IWarehouseReservationRepository>();
        repository
            .Setup(x => x.GetByOrderIdAsync(orderCreated.OrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Domain.Entities.WarehouseReservation?)null);

        var sut = new StockReservationService(repository.Object);
        var result = await sut.ReserveAsync(orderCreated, CancellationToken.None);

        Assert.True(result.IsReserved);
        repository.Verify(x => x.Add(It.IsAny<Domain.Entities.WarehouseReservation>()), Times.Once);
        repository.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReserveAsync_ExistingOrder_ReturnsExistingReservation()
    {
        var orderId = Guid.NewGuid();
        var existing = Domain.Entities.WarehouseReservation.Create(orderId, 100m, true, null);

        var orderCreated = new OrderCreatedV1(
            orderId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "stripe_card",
            100m,
            "Pending",
            new IntegrationEventMetadata(Guid.NewGuid(), DateTimeOffset.UtcNow, "corr-1", "Order"));

        var repository = new Mock<IWarehouseReservationRepository>();
        repository
            .Setup(x => x.GetByOrderIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var sut = new StockReservationService(repository.Object);
        var result = await sut.ReserveAsync(orderCreated, CancellationToken.None);

        Assert.True(result.IsReserved);
        repository.Verify(x => x.Add(It.IsAny<Domain.Entities.WarehouseReservation>()), Times.Never);
        repository.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}

