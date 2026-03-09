using Order.Application.Abstractions.Commands;
using Order.Application.Abstractions.Repositories;
using Order.Application.Abstractions.Rules;
using Order.Application.Commands;
using Order.Application.Views;
using Order.Domain.ValueObjects;
using Shared.BuildingBlocks.Contracts.IntegrationEvents;
using Shared.BuildingBlocks.Contracts.IntegrationEvents.Order;
using Shared.BuildingBlocks.Contracts.Messaging;
using Shared.BuildingBlocks.Exceptions;
using Shared.BuildingBlocks.Mapping;

namespace Order.Application.Services;

public sealed class OrderCommandService(
    IOrderRepository orderRepository,
    IOrderRules orderRules,
    IDomainEventPublisher eventPublisher,
    IViewMapper<Order.Domain.Entities.Order, OrderView> mapper) : IOrderCommandService
{
    public async Task<OrderView> CreateAsync(CreateOrderCommand command, CancellationToken cancellationToken)
    {
        orderRules.EnsureCreateCommandIsValid(command);

        var itemValueObjects = command.Items
            .Select(x => OrderItem.Create(x.ProductId, x.Sku, x.Name, x.Quantity, x.UnitPrice))
            .ToArray();

        var order = Domain.Entities.Order.Create(
            command.CartId,
            command.UserId,
            command.IdentityType,
            command.PaymentMethod,
            command.AuthenticatedUserId,
            command.AnonymousId,
            OrderCustomer.Create(command.Customer.FirstName, command.Customer.LastName, command.Customer.Email, command.Customer.Phone),
            OrderAddress.Create(command.ShippingAddress.Street, command.ShippingAddress.City, command.ShippingAddress.PostalCode, command.ShippingAddress.Country),
            OrderAddress.Create(command.BillingAddress.Street, command.BillingAddress.City, command.BillingAddress.PostalCode, command.BillingAddress.Country),
            itemValueObjects,
            command.TotalAmount);

        orderRepository.Add(order);

        var integrationEvent = new OrderCreatedV1(
            order.Id,
            order.CartId,
            order.UserId,
            order.PaymentMethod,
            order.TotalAmount,
            order.Status.ToString(),
            CreateMetadata(command.CorrelationId));

        await eventPublisher.PublishAndFlushAsync(integrationEvent, cancellationToken);

        var persistedOrder = await orderRepository.GetByIdAsync(order.Id, cancellationToken)
            ?? throw new NotFoundAppException($"Order '{order.Id}' not found.");

        return mapper.Map(persistedOrder);
    }

    public async Task<OrderView> ManualCompleteAsync(ManualCompleteOrderCommand command, CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdAsync(command.OrderId, cancellationToken)
            ?? throw new NotFoundAppException($"Order '{command.OrderId}' not found.");

        order.MarkCompleted(orderRules.NormalizeTrackingCode(command.TrackingCode), orderRules.NormalizeTransactionId(command.TransactionId));

        var completedEvent = new OrderCompletedV1(
            order.Id,
            order.CartId,
            order.UserId,
            order.TrackingCode,
            order.TransactionId,
            order.Customer.Email,
            order.TotalAmount,
            CreateMetadata("manual"));

        await eventPublisher.PublishAndFlushAsync(completedEvent, cancellationToken);
        await orderRepository.SaveChangesAsync(cancellationToken);

        return mapper.Map(order);
    }

    public async Task<OrderView> ManualCancelAsync(ManualCancelOrderCommand command, CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdAsync(command.OrderId, cancellationToken)
            ?? throw new NotFoundAppException($"Order '{command.OrderId}' not found.");

        order.MarkCancelled(orderRules.NormalizeCancelReason(command.Reason));
        await orderRepository.SaveChangesAsync(cancellationToken);

        return mapper.Map(order);
    }

    private static IntegrationEventMetadata CreateMetadata(string correlationId)
    {
        return new IntegrationEventMetadata(Guid.NewGuid(), DateTimeOffset.UtcNow, correlationId, "Order");
    }
}
