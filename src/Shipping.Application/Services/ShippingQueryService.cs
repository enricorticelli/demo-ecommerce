using Shared.BuildingBlocks.Exceptions;
using Shared.BuildingBlocks.Mapping;
using Shipping.Application.Abstractions.Queries;
using Shipping.Application.Abstractions.Repositories;
using Shipping.Application.Views;

namespace Shipping.Application.Services;

public sealed class ShippingQueryService(
    IShipmentRepository shipmentRepository,
    IViewMapper<Domain.Entities.Shipment, ShipmentView> mapper) : IShippingQueryService
{
    public async Task<IReadOnlyList<ShipmentView>> ListAsync(int limit, int offset, string? searchTerm, CancellationToken cancellationToken)
    {
        var shipments = await shipmentRepository.ListAsync(limit, offset, searchTerm, cancellationToken);
        return shipments.Select(mapper.Map).ToArray();
    }

    public async Task<ShipmentView> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var shipment = await shipmentRepository.GetByOrderIdAsync(orderId, cancellationToken)
            ?? throw new NotFoundAppException($"Shipment for order '{orderId}' not found.");

        return mapper.Map(shipment);
    }
}
