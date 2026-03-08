using Shipping.Api.Contracts.Requests;
using Shipping.Api.Contracts.Responses;
using Shipping.Application.Commands;
using Shipping.Application.Views;

namespace Shipping.Api.Mappers;

public static class ShippingMapper
{
	public static CreateShipmentCommand ToCreateCommand(this CreateShipmentRequest request)
	{
		return new CreateShipmentCommand(request.OrderId, request.UserId);
	}

	public static UpdateShipmentStatusCommand ToUpdateStatusCommand(this UpdateShipmentStatusRequest request, Guid shipmentId)
	{
		return new UpdateShipmentStatusCommand(shipmentId, request.Status);
	}

	public static ShipmentResponse ToResponse(this ShipmentView view)
	{
		return new ShipmentResponse(
			view.Id,
			view.OrderId,
			view.UserId,
			view.TrackingCode,
			view.Status,
			view.CreatedAtUtc,
			view.UpdatedAtUtc,
			view.DeliveredAtUtc);
	}
}
