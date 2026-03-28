using Moq;
using Order.Application.Abstractions.Repositories;
using Order.Application.Abstractions.Rules;
using Order.Application.Commands;
using Order.Application.Services;
using Order.Application.Views;
using Order.Domain.ValueObjects;
using Shared.BuildingBlocks.Contracts.IntegrationEvents;
using Shared.BuildingBlocks.Contracts.IntegrationEvents.Order;
using Shared.BuildingBlocks.Contracts.Messaging;
using Shared.BuildingBlocks.Mapping;
using Xunit;

namespace Order.Tests;

public sealed class OrderCommandServiceTests
{
    [Fact]
    public async Task CreateAsync_ValidCommand_AddsOrderPublishesEventAndReturnsView()
    {
        var command = BuildCreateCommand();

        var repository = new Mock<IOrderRepository>();
        repository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid _, CancellationToken _) => BuildOrderEntity());

        var rules = new Mock<IOrderRules>();
        var publisher = new Mock<IDomainEventPublisher>();
        publisher
            .Setup(x => x.PublishAndFlushAsync(It.IsAny<IntegrationEventBase>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        publisher
            .Setup(x => x.PublishBatchAndFlushAsync(It.IsAny<IReadOnlyCollection<IntegrationEventBase>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var mapper = new Mock<IViewMapper<Order.Domain.Entities.Order, OrderView>>();
        mapper
            .Setup(x => x.Map(It.IsAny<Order.Domain.Entities.Order>()))
            .Returns((Order.Domain.Entities.Order source) => new OrderView(
                source.Id,
                source.CartId,
                source.UserId,
                source.IdentityType,
                source.PaymentMethod,
                source.AuthenticatedUserId,
                source.AnonymousId,
                new OrderCustomerView(source.Customer.FirstName, source.Customer.LastName, source.Customer.Email, source.Customer.Phone),
                new OrderAddressView(source.ShippingAddress.Street, source.ShippingAddress.City, source.ShippingAddress.PostalCode, source.ShippingAddress.Country),
                new OrderAddressView(source.BillingAddress.Street, source.BillingAddress.City, source.BillingAddress.PostalCode, source.BillingAddress.Country),
                source.Status.ToString(),
                source.TotalAmount,
                source.Items.Select(i => new OrderItemView(i.ProductId, i.Sku, i.Name, i.Quantity, i.UnitPrice)).ToArray(),
                source.TrackingCode,
                source.TransactionId,
                source.FailureReason));

        var sut = new OrderCommandService(repository.Object, rules.Object, publisher.Object, mapper.Object);

        var result = await sut.CreateAsync(command, CancellationToken.None);

        Assert.Equal(command.TotalAmount, result.TotalAmount);
        Assert.Equal("Pending", result.Status);

        repository.Verify(x => x.Add(It.IsAny<Order.Domain.Entities.Order>()), Times.Once);
        rules.Verify(x => x.EnsureCreateCommandIsValid(command), Times.Once);
        publisher.Verify(
            x => x.PublishAndFlushAsync(It.Is<OrderCreatedV1>(e => e.Metadata.CorrelationId == command.CorrelationId), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AdminManualCompleteAsync_ValidCommand_SavesChangesAndReturnsCompletedView()
    {
        var existing = BuildOrderEntity();

        var repository = new Mock<IOrderRepository>();
        repository.Setup(x => x.GetByIdAsync(existing.Id, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var rules = new Mock<IOrderRules>();
        rules.Setup(x => x.NormalizeTrackingCode("TRK-1")).Returns("TRK-1");
        rules.Setup(x => x.NormalizeTransactionId("TX-1")).Returns("TX-1");

        var publisher = new Mock<IDomainEventPublisher>();
        publisher
            .Setup(x => x.PublishAndFlushAsync(It.IsAny<IntegrationEventBase>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var mapper = new Mock<IViewMapper<Order.Domain.Entities.Order, OrderView>>();
        mapper.Setup(x => x.Map(existing)).Returns(() => new OrderView(
            existing.Id,
            existing.CartId,
            existing.UserId,
            existing.IdentityType,
            existing.PaymentMethod,
            existing.AuthenticatedUserId,
            existing.AnonymousId,
            new OrderCustomerView(existing.Customer.FirstName, existing.Customer.LastName, existing.Customer.Email, existing.Customer.Phone),
            new OrderAddressView(existing.ShippingAddress.Street, existing.ShippingAddress.City, existing.ShippingAddress.PostalCode, existing.ShippingAddress.Country),
            new OrderAddressView(existing.BillingAddress.Street, existing.BillingAddress.City, existing.BillingAddress.PostalCode, existing.BillingAddress.Country),
            existing.Status.ToString(),
            existing.TotalAmount,
            existing.Items.Select(i => new OrderItemView(i.ProductId, i.Sku, i.Name, i.Quantity, i.UnitPrice)).ToArray(),
            existing.TrackingCode,
            existing.TransactionId,
            existing.FailureReason));

        var sut = new OrderCommandService(repository.Object, rules.Object, publisher.Object, mapper.Object);

        var result = await sut.AdminManualCompleteAsync(new ManualCompleteOrderCommand(existing.Id, "TRK-1", "TX-1"), CancellationToken.None);

        Assert.Equal("Completed", result.Status);
        Assert.Equal("TRK-1", result.TrackingCode);
        Assert.Equal("TX-1", result.TransactionId);
        publisher.Verify(
            x => x.PublishAndFlushAsync(
                It.Is<OrderCompletedV1>(e =>
                    e.OrderId == existing.Id
                    && e.CustomerEmail == existing.Customer.Email
                    && e.TotalAmount == existing.TotalAmount),
                It.IsAny<CancellationToken>()),
            Times.Once);
        repository.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StoreManualCancelAsync_ValidCommand_SavesChangesAndReturnsCancelledView()
    {
        var existing = BuildOrderEntity();

        var repository = new Mock<IOrderRepository>();
        repository.Setup(x => x.GetByIdAsync(existing.Id, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var rules = new Mock<IOrderRules>();
        rules.Setup(x => x.NormalizeCancelReason("No stock")).Returns("No stock");

        var publisher = new Mock<IDomainEventPublisher>();
        var mapper = new Mock<IViewMapper<Order.Domain.Entities.Order, OrderView>>();
        mapper.Setup(x => x.Map(existing)).Returns(() => new OrderView(
            existing.Id,
            existing.CartId,
            existing.UserId,
            existing.IdentityType,
            existing.PaymentMethod,
            existing.AuthenticatedUserId,
            existing.AnonymousId,
            new OrderCustomerView(existing.Customer.FirstName, existing.Customer.LastName, existing.Customer.Email, existing.Customer.Phone),
            new OrderAddressView(existing.ShippingAddress.Street, existing.ShippingAddress.City, existing.ShippingAddress.PostalCode, existing.ShippingAddress.Country),
            new OrderAddressView(existing.BillingAddress.Street, existing.BillingAddress.City, existing.BillingAddress.PostalCode, existing.BillingAddress.Country),
            existing.Status.ToString(),
            existing.TotalAmount,
            existing.Items.Select(i => new OrderItemView(i.ProductId, i.Sku, i.Name, i.Quantity, i.UnitPrice)).ToArray(),
            existing.TrackingCode,
            existing.TransactionId,
            existing.FailureReason));

        var sut = new OrderCommandService(repository.Object, rules.Object, publisher.Object, mapper.Object);

        var result = await sut.StoreManualCancelAsync(new ManualCancelOrderCommand(existing.Id, "No stock"), CancellationToken.None);

        Assert.Equal("Cancelled", result.Status);
        Assert.Equal("No stock", result.FailureReason);
        repository.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AdminManualCancelAsync_CompletedOrder_ForcesCancelledStatus()
    {
        var existing = BuildOrderEntity();
        existing.MarkCompleted("TRK-1", "TX-1");

        var repository = new Mock<IOrderRepository>();
        repository.Setup(x => x.GetByIdAsync(existing.Id, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var rules = new Mock<IOrderRules>();
        rules.Setup(x => x.NormalizeCancelReason("Admin override")).Returns("Admin override");

        var publisher = new Mock<IDomainEventPublisher>();
        var mapper = new Mock<IViewMapper<Order.Domain.Entities.Order, OrderView>>();
        mapper.Setup(x => x.Map(existing)).Returns(() => new OrderView(
            existing.Id,
            existing.CartId,
            existing.UserId,
            existing.IdentityType,
            existing.PaymentMethod,
            existing.AuthenticatedUserId,
            existing.AnonymousId,
            new OrderCustomerView(existing.Customer.FirstName, existing.Customer.LastName, existing.Customer.Email, existing.Customer.Phone),
            new OrderAddressView(existing.ShippingAddress.Street, existing.ShippingAddress.City, existing.ShippingAddress.PostalCode, existing.ShippingAddress.Country),
            new OrderAddressView(existing.BillingAddress.Street, existing.BillingAddress.City, existing.BillingAddress.PostalCode, existing.BillingAddress.Country),
            existing.Status.ToString(),
            existing.TotalAmount,
            existing.Items.Select(i => new OrderItemView(i.ProductId, i.Sku, i.Name, i.Quantity, i.UnitPrice)).ToArray(),
            existing.TrackingCode,
            existing.TransactionId,
            existing.FailureReason));

        var sut = new OrderCommandService(repository.Object, rules.Object, publisher.Object, mapper.Object);

        var result = await sut.AdminManualCancelAsync(new ManualCancelOrderCommand(existing.Id, "Admin override"), CancellationToken.None);

        Assert.Equal("Cancelled", result.Status);
        Assert.Equal("Admin override", result.FailureReason);
        repository.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private static CreateOrderCommand BuildCreateCommand()
    {
        return new CreateOrderCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "authenticated",
            "card",
            [new CreateOrderItemCommand(Guid.NewGuid(), "SKU-1", "Item 1", 2, 10m)],
            20m,
            Guid.NewGuid(),
            null,
            new CreateOrderCustomerCommand("Mario", "Rossi", "mario@example.com", "+39000000000"),
            new CreateOrderAddressCommand("Street 1", "Rome", "00100", "IT"),
            new CreateOrderAddressCommand("Street 1", "Rome", "00100", "IT"),
            "corr-123");
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
            OrderCustomer.Create("Mario", "Rossi", "mario@example.com", "+39000000000"),
            OrderAddress.Create("Street 1", "Rome", "00100", "IT"),
            OrderAddress.Create("Street 1", "Rome", "00100", "IT"),
            [OrderItem.Create(Guid.NewGuid(), "SKU-1", "Item 1", 2, 10m)],
            20m);
    }
}

