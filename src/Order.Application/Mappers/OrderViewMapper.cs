using Order.Application.Views;
using Shared.BuildingBlocks.Mapping;
using OrderEntity = Order.Domain.Entities.Order;

namespace Order.Application.Mappers;

public sealed class OrderViewMapper : IViewMapper<OrderEntity, OrderView>
{
    public OrderView Map(OrderEntity source)
    {
        return new OrderView(
            source.Id,
            source.CartId,
            source.UserId,
            source.IdentityType,
            source.PaymentMethod,
            source.AuthenticatedUserId,
            source.AnonymousId,
            new OrderCustomerView(
                source.Customer.FirstName,
                source.Customer.LastName,
                source.Customer.Email,
                source.Customer.Phone),
            new OrderAddressView(
                source.ShippingAddress.Street,
                source.ShippingAddress.City,
                source.ShippingAddress.PostalCode,
                source.ShippingAddress.Country),
            new OrderAddressView(
                source.BillingAddress.Street,
                source.BillingAddress.City,
                source.BillingAddress.PostalCode,
                source.BillingAddress.Country),
            source.Status.ToString(),
            source.TotalAmount,
            source.Items
                .Select(x => new OrderItemView(x.ProductId, x.Sku, x.Name, x.Quantity, x.UnitPrice))
                .ToArray(),
            source.TrackingCode,
            source.TransactionId,
            source.FailureReason,
            source.CreatedAtUtc);
    }
}
