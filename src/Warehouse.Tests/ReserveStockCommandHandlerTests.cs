using Moq;
using Shared.BuildingBlocks.Contracts;
using Warehouse.Application;
using Xunit;

namespace Warehouse.Tests;

public sealed class ReserveStockCommandHandlerTests
{
    [Fact]
    public async Task Reserve_stock_command_should_delegate_to_warehouse_service()
    {
        var request = new StockReserveRequestedV1(Guid.NewGuid(), []);
        var expected = new StockReservationResult(request.OrderId, true);

        var service = new Mock<IWarehouseService>();
        service.Setup(x => x.ReserveStockAsync(request, It.IsAny<CancellationToken>())).ReturnsAsync(expected);

        var sut = new ReserveStockCommandHandler(service.Object);
        var actual = await sut.HandleAsync(new ReserveStockCommand(request), CancellationToken.None);

        Assert.Equal(expected, actual);
    }
}
