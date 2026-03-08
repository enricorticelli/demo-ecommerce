using Moq;
using Shared.BuildingBlocks.Exceptions;
using Shared.BuildingBlocks.Mapping;
using Shipping.Application.Abstractions.Repositories;
using Shipping.Application.Commands;
using Shipping.Application.Services;
using Shipping.Application.Views;
using Xunit;

namespace Shipping.Tests;

public sealed class CreateShipmentCommandHandlerTests
{
    [Fact]
    public async Task Create_should_return_existing_shipment_if_order_already_exists()
    {
        var existing = Domain.Entities.Shipment.Create(Guid.NewGuid(), Guid.NewGuid());
        var repository = new Mock<IShipmentRepository>();
        repository
            .Setup(x => x.GetByOrderIdAsync(existing.OrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var mapper = new Mock<IViewMapper<Domain.Entities.Shipment, ShipmentView>>();
        mapper.Setup(x => x.Map(existing)).Returns(ToView(existing));

        var sut = new ShippingCommandService(repository.Object, mapper.Object);
        var result = await sut.CreateAsync(new CreateShipmentCommand(existing.OrderId, existing.UserId), CancellationToken.None);

        Assert.Equal(existing.OrderId, result.OrderId);
        repository.Verify(x => x.Add(It.IsAny<Domain.Entities.Shipment>()), Times.Never);
        repository.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Update_status_should_throw_when_shipment_does_not_exist()
    {
        var repository = new Mock<IShipmentRepository>();
        repository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Domain.Entities.Shipment?)null);

        var mapper = new Mock<IViewMapper<Domain.Entities.Shipment, ShipmentView>>();
        var sut = new ShippingCommandService(repository.Object, mapper.Object);

        var action = async () => await sut.UpdateStatusAsync(new UpdateShipmentStatusCommand(Guid.NewGuid(), "InTransit"), CancellationToken.None);

        await Assert.ThrowsAsync<NotFoundAppException>(action);
    }

    [Fact]
    public async Task Update_status_should_persist_status_transition()
    {
        var shipment = Domain.Entities.Shipment.Create(Guid.NewGuid(), Guid.NewGuid());
        var repository = new Mock<IShipmentRepository>();
        repository
            .Setup(x => x.GetByIdAsync(shipment.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(shipment);

        var mapper = new Mock<IViewMapper<Domain.Entities.Shipment, ShipmentView>>();
        mapper.Setup(x => x.Map(shipment)).Returns(() => ToView(shipment));

        var sut = new ShippingCommandService(repository.Object, mapper.Object);
        var result = await sut.UpdateStatusAsync(new UpdateShipmentStatusCommand(shipment.Id, "InTransit"), CancellationToken.None);

        Assert.Equal("InTransit", result.Status);
        repository.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task List_should_map_shipments()
    {
        var shipment = Domain.Entities.Shipment.Create(Guid.NewGuid(), Guid.NewGuid());
        var repository = new Mock<IShipmentRepository>();
        repository
            .Setup(x => x.ListAsync(20, 0, "TRK", It.IsAny<CancellationToken>()))
            .ReturnsAsync([shipment]);

        var mapper = new Mock<IViewMapper<Domain.Entities.Shipment, ShipmentView>>();
        mapper.Setup(x => x.Map(shipment)).Returns(ToView(shipment));

        var sut = new ShippingQueryService(repository.Object, mapper.Object);
        var result = await sut.ListAsync(20, 0, "TRK", CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(shipment.Id, result[0].Id);
    }

    private static ShipmentView ToView(Domain.Entities.Shipment shipment)
    {
        return new ShipmentView(
            shipment.Id,
            shipment.OrderId,
            shipment.UserId,
            shipment.TrackingCode,
            shipment.Status,
            shipment.CreatedAtUtc,
            shipment.UpdatedAtUtc,
            shipment.DeliveredAtUtc);
    }
}
