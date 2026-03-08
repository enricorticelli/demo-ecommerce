using Moq;
using Order.Application.Abstractions.Repositories;
using Order.Application.Services;
using Order.Application.Views;
using Shared.BuildingBlocks.Exceptions;
using Shared.BuildingBlocks.Mapping;
using Xunit;

namespace Order.Tests;

public sealed class OrderQueryServiceTests
{
    [Fact]
    public async Task List_should_return_mapped_orders()
    {
        var order = BuildOrderEntity();

        var repository = new Mock<IOrderRepository>();
        repository.Setup(x => x.ListAsync(It.IsAny<CancellationToken>())).ReturnsAsync([order]);

        var mapper = new Mock<IViewMapper<Order.Domain.Entities.Order, OrderView>>();
        mapper
            .Setup(x => x.Map(order))
            .Returns(new OrderView(
                order.Id,
                order.CartId,
                order.UserId,
                order.IdentityType,
                order.PaymentMethod,
                order.AuthenticatedUserId,
                order.AnonymousId,
                new OrderCustomerView(order.Customer.FirstName, order.Customer.LastName, order.Customer.Email, order.Customer.Phone),
                new OrderAddressView(order.ShippingAddress.Street, order.ShippingAddress.City, order.ShippingAddress.PostalCode, order.ShippingAddress.Country),
                new OrderAddressView(order.BillingAddress.Street, order.BillingAddress.City, order.BillingAddress.PostalCode, order.BillingAddress.Country),
                order.Status.ToString(),
                order.TotalAmount,
                order.Items.Select(i => new OrderItemView(i.ProductId, i.Sku, i.Name, i.Quantity, i.UnitPrice)).ToArray(),
                order.TrackingCode,
                order.TransactionId,
                order.FailureReason));

        var sut = new OrderQueryService(repository.Object, mapper.Object);

        var result = await sut.ListAsync(CancellationToken.None);

        Assert.Single(result);
        mapper.Verify(x => x.Map(order), Times.Once);
    }

    [Fact]
    public async Task GetById_should_throw_when_order_does_not_exist()
    {
        var repository = new Mock<IOrderRepository>();
        repository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Order.Domain.Entities.Order?)null);

        var mapper = new Mock<IViewMapper<Order.Domain.Entities.Order, OrderView>>();
        var sut = new OrderQueryService(repository.Object, mapper.Object);

        var action = async () => await sut.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        await Assert.ThrowsAsync<NotFoundAppException>(action);
    }

    private static Order.Domain.Entities.Order BuildOrderEntity()
    {
        return Domain.Entities.Order.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "authenticated",
            "card",
            Guid.NewGuid(),
            null,
            Domain.ValueObjects.OrderCustomer.Create("Mario", "Rossi", "mario@example.com", "+39000000000"),
            Domain.ValueObjects.OrderAddress.Create("Street 1", "Rome", "00100", "IT"),
            Domain.ValueObjects.OrderAddress.Create("Street 1", "Rome", "00100", "IT"),
            [Domain.ValueObjects.OrderItem.Create(Guid.NewGuid(), "SKU-1", "Item 1", 2, 10m)],
            20m);
    }
}
