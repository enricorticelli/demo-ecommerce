namespace Shipping.Application.Commands;

public sealed record CreateShipmentCommand(Guid OrderId, Guid UserId);
