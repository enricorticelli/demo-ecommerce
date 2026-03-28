using Shared.BuildingBlocks.Contracts.IntegrationEvents.Order;
using Warehouse.Application.Abstractions.Repositories;
using Warehouse.Application.Abstractions.Services;

namespace Warehouse.Application.Services;

public sealed class StockReservationService(IWarehouseReservationRepository reservationRepository) : IStockReservationService
{
    public async Task<StockReservationResult> ReserveAsync(OrderCreatedV1 orderCreatedEvent, CancellationToken cancellationToken)
    {
        var existingReservation = await reservationRepository.GetByOrderIdAsync(orderCreatedEvent.OrderId, cancellationToken);
        if (existingReservation is not null)
        {
            return new StockReservationResult(
                existingReservation.OrderId,
                existingReservation.IsReserved,
                existingReservation.FailureReason);
        }

        var isReserved = orderCreatedEvent.TotalAmount > 0;
        var failureReason = isReserved ? null : "Invalid order amount.";

        var reservation = Domain.Entities.WarehouseReservation.Create(
            orderCreatedEvent.OrderId,
            orderCreatedEvent.TotalAmount,
            isReserved,
            failureReason);

        reservationRepository.Add(reservation);
        await reservationRepository.SaveChangesAsync(cancellationToken);

        return new StockReservationResult(reservation.OrderId, reservation.IsReserved, reservation.FailureReason);
    }
}
