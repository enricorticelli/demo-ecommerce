using Microsoft.EntityFrameworkCore;
using Shipping.Application.Abstractions.Repositories;
using ShippingEntity = Shipping.Domain.Entities.Shipment;

namespace Shipping.Infrastructure.Persistence.Repositories;

public sealed class ShipmentRepository(ShippingDbContext dbContext) : IShipmentRepository
{
    public void Add(ShippingEntity shipment)
    {
        dbContext.Shipments.Add(shipment);
    }

    public Task<ShippingEntity?> GetByIdAsync(Guid shipmentId, CancellationToken cancellationToken)
    {
        return dbContext.Shipments.FirstOrDefaultAsync(x => x.Id == shipmentId, cancellationToken);
    }

    public Task<ShippingEntity?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken)
    {
        return dbContext.Shipments.FirstOrDefaultAsync(x => x.OrderId == orderId, cancellationToken);
    }

    public async Task<IReadOnlyList<ShippingEntity>> ListAsync(int limit, int offset, string? searchTerm, CancellationToken cancellationToken)
    {
        var query = dbContext.Shipments.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var token = searchTerm.Trim();
            query = query.Where(x =>
                x.TrackingCode.Contains(token) ||
                x.Status.Contains(token) ||
                x.OrderId.ToString().Contains(token) ||
                x.UserId.ToString().Contains(token));
        }

        return await query
            .OrderByDescending(x => x.UpdatedAtUtc)
            .Skip(offset)
            .Take(limit)
            .ToArrayAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
