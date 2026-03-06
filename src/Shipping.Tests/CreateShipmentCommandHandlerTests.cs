using Moq;
using Shared.BuildingBlocks.Contracts;
using Shipping.Application;
using Xunit;

namespace Shipping.Tests;

public sealed class CreateShipmentCommandHandlerTests
{
    [Fact]
    public async Task Create_shipment_command_should_delegate_to_shipping_service()
    {
        var request = new ShippingCreateRequestedV1(Guid.NewGuid(), Guid.NewGuid(), []);
        var expected = new ShipmentResult(request.OrderId, "TRK-1");

        var service = new Mock<IShippingService>();
        service.Setup(x => x.CreateShipmentAsync(request, It.IsAny<CancellationToken>())).ReturnsAsync(expected);

        var sut = new CreateShipmentCommandHandler(service.Object);
        var actual = await sut.HandleAsync(new CreateShipmentCommand(request), CancellationToken.None);

        Assert.Equal(expected, actual);
    }
}
