using Shared.BuildingBlocks.Contracts.IntegrationEvents.Order;
using Warehouse.Application.Services;

namespace Warehouse.Application.Abstractions.Services;

public interface IStockReservationService
{
    Task<StockReservationResult> ReserveAsync(OrderCreatedV1 orderCreatedEvent, CancellationToken cancellationToken);
}
