using FluentAssertions;
using Order.Api.Domain;
using Shared.BuildingBlocks.Contracts;

namespace Order.UnitTests;

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

        aggregate.Status.Should().Be(OrderStatus.Completed);
        aggregate.TransactionId.Should().Be("TX-1");
        aggregate.TrackingCode.Should().Be("TRK-1");
    }
}
