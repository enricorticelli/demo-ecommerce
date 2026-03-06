using Shared.BuildingBlocks.Contracts;
using Shared.BuildingBlocks.Cqrs;

namespace Shipping.Application;

public sealed record CreateShipmentCommand(ShippingCreateRequestedV1 Request) : ICommand<ShipmentResult>;
