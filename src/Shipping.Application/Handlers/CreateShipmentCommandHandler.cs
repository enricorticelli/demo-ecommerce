using Shared.BuildingBlocks.Cqrs;

namespace Shipping.Application;

public sealed class CreateShipmentCommandHandler(IShippingService shippingService)
    : ICommandHandler<CreateShipmentCommand, ShipmentResult>
{
    public Task<ShipmentResult> HandleAsync(CreateShipmentCommand command, CancellationToken cancellationToken)
    {
        return shippingService.CreateShipmentAsync(command.Request, cancellationToken);
    }
}
