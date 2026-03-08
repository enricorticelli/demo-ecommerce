using Order.Domain.Entities;
using Order.Domain.ValueObjects;
using Shared.BuildingBlocks.Exceptions;
using Xunit;

namespace Order.Tests;

public sealed class OrderDomainTests
{
    [Fact]
    public void Create_should_set_pending_status_for_valid_order()
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
    public void Create_should_throw_when_total_does_not_match_items_sum()
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
    public void MarkCompleted_should_throw_for_non_pending_order()
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
    public void Order_should_move_to_completed_when_payment_and_stock_are_confirmed()
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
}
