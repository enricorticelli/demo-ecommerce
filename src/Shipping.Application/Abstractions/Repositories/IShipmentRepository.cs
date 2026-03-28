using ShippingEntity = Shipping.Domain.Entities.Shipment;

namespace Shipping.Application.Abstractions.Repositories;

public interface IShipmentRepository
{
    void Add(ShippingEntity shipment);
    Task<ShippingEntity?> GetByIdAsync(Guid shipmentId, CancellationToken cancellationToken);
    Task<ShippingEntity?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken);
    Task<IReadOnlyList<ShippingEntity>> ListAsync(int limit, int offset, string? searchTerm, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
