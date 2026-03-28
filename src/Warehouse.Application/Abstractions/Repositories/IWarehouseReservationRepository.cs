using WarehouseReservationEntity = Warehouse.Domain.Entities.WarehouseReservation;

namespace Warehouse.Application.Abstractions.Repositories;

public interface IWarehouseReservationRepository
{
    Task<WarehouseReservationEntity?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken);
    void Add(WarehouseReservationEntity reservation);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
