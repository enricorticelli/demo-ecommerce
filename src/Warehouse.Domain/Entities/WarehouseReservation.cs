using Shared.BuildingBlocks.Exceptions;

namespace Warehouse.Domain.Entities;

public sealed class WarehouseReservation
{
    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public decimal TotalAmount { get; private set; }
    public bool IsReserved { get; private set; }
    public string? FailureReason { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    private WarehouseReservation()
    {
    }

    public static WarehouseReservation Create(Guid orderId, decimal totalAmount, bool isReserved, string? failureReason)
    {
        if (orderId == Guid.Empty)
        {
            throw new ValidationAppException("Order id is required.");
        }

        return new WarehouseReservation
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            TotalAmount = totalAmount,
            IsReserved = isReserved,
            FailureReason = failureReason,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
    }
}
