using Shared.BuildingBlocks.Mapping;
using Shipping.Application.Views;
using ShippingEntity = Shipping.Domain.Entities.Shipment;

namespace Shipping.Application.Mappers;

public sealed class ShipmentViewMapper : IViewMapper<ShippingEntity, ShipmentView>
{
    public ShipmentView Map(ShippingEntity source)
    {
        return new ShipmentView(
            source.Id,
            source.OrderId,
            source.UserId,
            source.TrackingCode,
            source.Status,
            source.CreatedAtUtc,
            source.UpdatedAtUtc,
            source.DeliveredAtUtc);
    }
}
