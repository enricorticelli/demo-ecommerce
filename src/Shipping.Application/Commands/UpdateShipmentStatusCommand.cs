namespace Shipping.Application.Commands;

public sealed record UpdateShipmentStatusCommand(Guid ShipmentId, string Status);
