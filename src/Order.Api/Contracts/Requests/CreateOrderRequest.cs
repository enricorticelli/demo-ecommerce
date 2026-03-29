namespace Order.Api.Contracts.Requests;

public sealed record CreateOrderRequest(
    Guid CartId,
    Guid UserId,
    string IdentityType,
    string PaymentMethod,
    IReadOnlyList<OrderItemRequest> Items,
    decimal TotalAmount,
    Guid? AuthenticatedUserId,
    Guid? AnonymousId,
    OrderCustomerRequest? Customer,
    OrderAddressRequest? ShippingAddress,
    OrderAddressRequest? BillingAddress);
