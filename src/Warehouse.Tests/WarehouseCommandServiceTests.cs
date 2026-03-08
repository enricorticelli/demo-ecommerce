using Moq;
using Warehouse.Application.Abstractions.Repositories;
using Warehouse.Application.Commands;
using Warehouse.Application.Services;
using Xunit;

namespace Warehouse.Tests;

public sealed class WarehouseCommandServiceTests
{
    [Fact]
    public async Task Upsert_should_create_stock_item_when_missing()
    {
        var stockRepository = new Mock<IWarehouseStockRepository>();
        stockRepository
            .Setup(x => x.GetByProductIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Domain.Entities.WarehouseStockItem?)null);

        var reservationRepository = new Mock<IWarehouseReservationRepository>();

        var sut = new WarehouseCommandService(stockRepository.Object, reservationRepository.Object);
        var result = await sut.UpsertStockAsync(new UpsertStockCommand(Guid.NewGuid(), "SKU-1", 10), CancellationToken.None);

        Assert.Equal("SKU-1", result.Sku);
        Assert.Equal(10, result.AvailableQuantity);
        stockRepository.Verify(x => x.Add(It.IsAny<Domain.Entities.WarehouseStockItem>()), Times.Once);
        stockRepository.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Reserve_should_fail_when_stock_is_missing()
    {
        var orderId = Guid.NewGuid();

        var stockRepository = new Mock<IWarehouseStockRepository>();
        stockRepository
            .Setup(x => x.GetByProductIdsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Domain.Entities.WarehouseStockItem>());

        var reservationRepository = new Mock<IWarehouseReservationRepository>();
        reservationRepository
            .Setup(x => x.GetByOrderIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Domain.Entities.WarehouseReservation?)null);

        var sut = new WarehouseCommandService(stockRepository.Object, reservationRepository.Object);

        var result = await sut.ReserveStockAsync(
            new ReserveStockCommand(orderId, [new ReserveStockItemCommand(Guid.NewGuid(), "SKU-1", "Item", 1, 10m)]),
            CancellationToken.None);

        Assert.False(result.Reserved);
        Assert.Contains("Missing stock", result.Reason, StringComparison.Ordinal);
        reservationRepository.Verify(x => x.Add(It.Is<Domain.Entities.WarehouseReservation>(r => !r.IsReserved)), Times.Once);
    }

    [Fact]
    public async Task Reserve_should_decrement_stock_when_available()
    {
        var productId = Guid.NewGuid();
        var stock = Domain.Entities.WarehouseStockItem.Create(productId, "SKU-1", 5);

        var stockRepository = new Mock<IWarehouseStockRepository>();
        stockRepository
            .Setup(x => x.GetByProductIdsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([stock]);

        var reservationRepository = new Mock<IWarehouseReservationRepository>();
        reservationRepository
            .Setup(x => x.GetByOrderIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Domain.Entities.WarehouseReservation?)null);

        var sut = new WarehouseCommandService(stockRepository.Object, reservationRepository.Object);

        var result = await sut.ReserveStockAsync(
            new ReserveStockCommand(Guid.NewGuid(), [new ReserveStockItemCommand(productId, "SKU-1", "Item", 2, 10m)]),
            CancellationToken.None);

        Assert.True(result.Reserved);
        Assert.Equal(3, stock.AvailableQuantity);
        stockRepository.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        reservationRepository.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
