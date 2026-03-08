using Shared.BuildingBlocks.Exceptions;
using Shared.BuildingBlocks.Mapping;
using Shipping.Application.Abstractions.Commands;
using Shipping.Application.Abstractions.Repositories;
using Shipping.Application.Commands;
using Shipping.Application.Views;

namespace Shipping.Application.Services;

public sealed class ShippingCommandService(
    IShipmentRepository shipmentRepository,
    IViewMapper<Shipping.Domain.Entities.Shipment, ShipmentView> mapper) : IShippingCommandService
{
    public async Task<ShipmentView> CreateAsync(CreateShipmentCommand command, CancellationToken cancellationToken)
    {
        var existing = await shipmentRepository.GetByOrderIdAsync(command.OrderId, cancellationToken);
        if (existing is not null)
        {
            return mapper.Map(existing);
        }

        var shipment = Shipping.Domain.Entities.Shipment.Create(command.OrderId, command.UserId);
        shipmentRepository.Add(shipment);
        await shipmentRepository.SaveChangesAsync(cancellationToken);

        return mapper.Map(shipment);
    }

    public async Task<ShipmentView> UpdateStatusAsync(UpdateShipmentStatusCommand command, CancellationToken cancellationToken)
    {
        var shipment = await shipmentRepository.GetByIdAsync(command.ShipmentId, cancellationToken)
            ?? throw new NotFoundAppException($"Shipment '{command.ShipmentId}' not found.");

        shipment.UpdateStatus(command.Status.Trim());
        await shipmentRepository.SaveChangesAsync(cancellationToken);

        return mapper.Map(shipment);
    }
}
