namespace Order.Application.Views;

public sealed record OrderView(
    Guid Id,
    Guid CartId,
    Guid UserId,
    string IdentityType,
    string PaymentMethod,
    Guid? AuthenticatedUserId,
    Guid? AnonymousId,
    OrderCustomerView Customer,
    OrderAddressView ShippingAddress,
    OrderAddressView BillingAddress,
    string Status,
    decimal TotalAmount,
    IReadOnlyList<OrderItemView> Items,
    string TrackingCode,
    string TransactionId,
    string FailureReason,
    DateTimeOffset CreatedAtUtc = default);
