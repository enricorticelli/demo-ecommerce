using Shared.BuildingBlocks.Exceptions;
using Warehouse.Application.Abstractions.Commands;
using Warehouse.Application.Abstractions.Repositories;
using Warehouse.Application.Commands;
using Warehouse.Application.Views;

namespace Warehouse.Application.Services;

public sealed class WarehouseCommandService(
    IWarehouseStockRepository stockRepository,
    IWarehouseReservationRepository reservationRepository) : IWarehouseCommandService
{
    public async Task<WarehouseStockView> UpsertStockAsync(UpsertStockCommand command, CancellationToken cancellationToken)
    {
        var stockItem = await stockRepository.GetByProductIdAsync(command.ProductId, cancellationToken);
        if (stockItem is null)
        {
            stockItem = Domain.Entities.WarehouseStockItem.Create(command.ProductId, command.Sku, command.AvailableQuantity);
            stockRepository.Add(stockItem);
        }
        else
        {
            stockItem.Upsert(command.Sku, command.AvailableQuantity);
        }

        await stockRepository.SaveChangesAsync(cancellationToken);
        return new WarehouseStockView(stockItem.ProductId, stockItem.Sku, stockItem.AvailableQuantity);
    }

    public async Task<WarehouseReserveView> ReserveStockAsync(ReserveStockCommand command, CancellationToken cancellationToken)
    {
        if (command.OrderId == Guid.Empty)
        {
            throw new ValidationAppException("Order id is required.");
        }

        var existingReservation = await reservationRepository.GetByOrderIdAsync(command.OrderId, cancellationToken);
        if (existingReservation is not null)
        {
            return new WarehouseReserveView(existingReservation.OrderId, existingReservation.IsReserved, existingReservation.FailureReason);
        }

        if (command.Items.Count == 0 || command.Items.Any(x => x.Quantity <= 0))
        {
            return await SaveFailedReservationAsync(command, "Invalid quantity", cancellationToken);
        }

        var productIds = command.Items.Select(x => x.ProductId).Distinct().ToArray();
        var stockItems = await stockRepository.GetByProductIdsAsync(productIds, cancellationToken);
        var stockByProduct = stockItems.ToDictionary(x => x.ProductId);

        foreach (var item in command.Items)
        {
            if (!stockByProduct.TryGetValue(item.ProductId, out var stockItem))
            {
                return await SaveFailedReservationAsync(command, $"Missing stock for SKU '{item.Sku}'.", cancellationToken);
            }

            if (!stockItem.TryReserve(item.Quantity))
            {
                return await SaveFailedReservationAsync(command, $"Insufficient stock for SKU '{item.Sku}'.", cancellationToken);
            }
        }

        var amount = command.Items.Sum(x => x.Quantity * x.UnitPrice);
        var reservation = Domain.Entities.WarehouseReservation.Create(command.OrderId, amount, true, null);
        reservationRepository.Add(reservation);

        await stockRepository.SaveChangesAsync(cancellationToken);
        await reservationRepository.SaveChangesAsync(cancellationToken);

        return new WarehouseReserveView(command.OrderId, true, null);
    }

    private async Task<WarehouseReserveView> SaveFailedReservationAsync(
        ReserveStockCommand command,
        string reason,
        CancellationToken cancellationToken)
    {
        var amount = command.Items.Sum(x => x.Quantity * x.UnitPrice);
        var reservation = Domain.Entities.WarehouseReservation.Create(command.OrderId, amount, false, reason);
        reservationRepository.Add(reservation);
        await reservationRepository.SaveChangesAsync(cancellationToken);
        return new WarehouseReserveView(command.OrderId, false, reason);
    }
}
