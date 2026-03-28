using Order.Domain.Entities;
using Order.Domain.ValueObjects;
using Shared.BuildingBlocks.Exceptions;
using Xunit;

namespace Order.Tests;

public sealed class OrderDomainTests
{
    [Fact]
    public void Create_ValidOrder_SetsPendingStatus()
    {
        var item = OrderItem.Create(Guid.NewGuid(), "SKU-1", "Item 1", 2, 10m);
        var customer = OrderCustomer.Create("Mario", "Rossi", "mario@example.com", "+39000000000");
        var shippingAddress = OrderAddress.Create("Street 1", "Rome", "00100", "IT");
        var billingAddress = OrderAddress.Create("Street 2", "Milan", "20100", "IT");

        var order = Order.Domain.Entities.Order.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "authenticated",
            "card",
            Guid.NewGuid(),
            null,
            customer,
            shippingAddress,
            billingAddress,
            [item],
            20m);

        Assert.Equal(OrderStatus.Pending, order.Status);
        Assert.Equal(20m, order.TotalAmount);
        Assert.Single(order.Items);
    }

    [Fact]
    public void Create_TotalMismatch_ThrowsValidationException()
    {
        var item = OrderItem.Create(Guid.NewGuid(), "SKU-1", "Item 1", 1, 10m);
        var customer = OrderCustomer.Create("Mario", "Rossi", "mario@example.com", "+39000000000");
        var address = OrderAddress.Create("Street 1", "Rome", "00100", "IT");

        var action = () => Order.Domain.Entities.Order.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "authenticated",
            "card",
            Guid.NewGuid(),
            null,
            customer,
            address,
            address,
            [item],
            11m);

        Assert.Throws<ValidationAppException>(action);
    }

    [Fact]
    public void CreateCustomer_EmptyPhone_IsAllowed()
    {
        var customer = OrderCustomer.Create("Mario", "Rossi", "mario@example.com", "   ");

        Assert.Equal(string.Empty, customer.Phone);
    }

    [Fact]
    public void MarkCompleted_NonPendingOrder_ThrowsConflictException()
    {
        var item = OrderItem.Create(Guid.NewGuid(), "SKU-1", "Item 1", 1, 10m);
        var customer = OrderCustomer.Create("Mario", "Rossi", "mario@example.com", "+39000000000");
        var address = OrderAddress.Create("Street 1", "Rome", "00100", "IT");
        var order = Order.Domain.Entities.Order.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "authenticated",
            "card",
            Guid.NewGuid(),
            null,
            customer,
            address,
            address,
            [item],
            10m);

        order.MarkCancelled("No stock");

        var action = () => order.MarkCompleted("TRK", "TX");

        Assert.Throws<ConflictAppException>(action);
    }

    [Fact]
    public void ApplySignals_PaymentAndStockConfirmed_MovesToCompleted()
    {
        var item = OrderItem.Create(Guid.NewGuid(), "SKU-1", "Item 1", 1, 10m);
        var customer = OrderCustomer.Create("Mario", "Rossi", "mario@example.com", "+39000000000");
        var address = OrderAddress.Create("Street 1", "Rome", "00100", "IT");
        var order = Order.Domain.Entities.Order.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "authenticated",
            "card",
            Guid.NewGuid(),
            null,
            customer,
            address,
            address,
            [item],
            10m);

        order.ApplyPaymentAuthorized("TX-1");
        order.ApplyStockReserved();

        Assert.Equal(OrderStatus.Completed, order.Status);
        Assert.True(order.IsPaymentAuthorized);
        Assert.True(order.IsStockReserved);
        Assert.Equal("TX-1", order.TransactionId);
    }

    [Fact]
    public void ForceMarkCancelled_CompletedOrder_ChangesStatusToCancelled()
    {
        var item = OrderItem.Create(Guid.NewGuid(), "SKU-1", "Item 1", 1, 10m);
        var customer = OrderCustomer.Create("Mario", "Rossi", "mario@example.com", "+39000000000");
        var address = OrderAddress.Create("Street 1", "Rome", "00100", "IT");
        var order = Order.Domain.Entities.Order.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "authenticated",
            "card",
            Guid.NewGuid(),
            null,
            customer,
            address,
            address,
            [item],
            10m);

        order.MarkCompleted("TRK", "TX");
        order.ForceMarkCancelled("Admin override");

        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Equal("Admin override", order.FailureReason);
    }

    [Fact]
    public void ForceMarkCompleted_CancelledOrder_ChangesStatusToCompleted()
    {
        var item = OrderItem.Create(Guid.NewGuid(), "SKU-1", "Item 1", 1, 10m);
        var customer = OrderCustomer.Create("Mario", "Rossi", "mario@example.com", "+39000000000");
        var address = OrderAddress.Create("Street 1", "Rome", "00100", "IT");
        var order = Order.Domain.Entities.Order.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "authenticated",
            "card",
            Guid.NewGuid(),
            null,
            customer,
            address,
            address,
            [item],
            10m);

        order.MarkCancelled("No stock");
        order.ForceMarkCompleted("TRK-ADMIN", "TX-ADMIN");

        Assert.Equal(OrderStatus.Completed, order.Status);
        Assert.Equal("TRK-ADMIN", order.TrackingCode);
        Assert.Equal("TX-ADMIN", order.TransactionId);
    }
}

