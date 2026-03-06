using Shared.BuildingBlocks.Contracts;
using Shipping.Application;

namespace Shipping.Infrastructure;

public sealed class ShippingCreateRequestedHandler
{
    public static async Task Handle(ShippingCreateRequestedV1 message, IShippingService shippingService, CancellationToken cancellationToken)
    {
        await shippingService.CreateShipmentAsync(message, cancellationToken);
    }
}
