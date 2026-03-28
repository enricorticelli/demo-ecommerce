using Microsoft.EntityFrameworkCore;
using Warehouse.Application.Abstractions.Repositories;
using WarehouseReservationEntity = Warehouse.Domain.Entities.WarehouseReservation;

namespace Warehouse.Infrastructure.Persistence.Repositories;

public sealed class WarehouseReservationRepository(WarehouseDbContext dbContext) : IWarehouseReservationRepository
{
    public Task<WarehouseReservationEntity?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken)
    {
        return dbContext.WarehouseReservations
            .FirstOrDefaultAsync(x => x.OrderId == orderId, cancellationToken);
    }

    public void Add(WarehouseReservationEntity reservation)
    {
        dbContext.WarehouseReservations.Add(reservation);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
