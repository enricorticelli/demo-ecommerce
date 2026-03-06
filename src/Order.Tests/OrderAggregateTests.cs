using Moq;
using Order.Domain;
using Shared.BuildingBlocks.Contracts;
using Xunit;

namespace Order.Tests;

public sealed class OrderAggregateTests
{
    [Fact]
    public void Should_transition_to_completed_when_full_flow_events_are_applied()
    {
        var orderId = Guid.NewGuid();
        var aggregate = new OrderAggregate();

        aggregate.Apply(new OrderPlacedDomain(orderId, Guid.NewGuid(), Guid.NewGuid(),
            [new OrderItemDto(Guid.NewGuid(), "SKU-1", "Product", 1, 99m)],
            99m));
        aggregate.Apply(new OrderStockReservedDomain(orderId));
        aggregate.Apply(new OrderPaymentAuthorizedDomain(orderId, "TX-1"));
        aggregate.Apply(new OrderCompletedDomain(orderId, "TRK-1", "TX-1"));

        Assert.Equal(OrderStatus.Completed, aggregate.Status);
        Assert.Equal("TX-1", aggregate.TransactionId);
        Assert.Equal("TRK-1", aggregate.TrackingCode);
    }

    [Fact]
    public void Moq_should_verify_test_probe_invocation()
    {
        var probe = new Mock<IOrderTestProbe>();
        probe.Object.Mark("order-tests");

        probe.Verify(x => x.Mark("order-tests"), Times.Once);
    }
}
