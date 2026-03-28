using Shipping.Application.Commands;
using Shipping.Application.Views;

namespace Shipping.Application.Abstractions.Commands;

public interface IShippingCommandService
{
    Task<ShipmentView> CreateAsync(CreateShipmentCommand command, CancellationToken cancellationToken);
    Task<ShipmentView> UpdateStatusAsync(UpdateShipmentStatusCommand command, CancellationToken cancellationToken);
}
