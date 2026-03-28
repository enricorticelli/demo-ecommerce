namespace Order.Api.Contracts.Responses;

public sealed record OrderResponse(
    Guid Id,
    Guid CartId,
    Guid UserId,
    string IdentityType,
    string PaymentMethod,
    Guid? AuthenticatedUserId,
    Guid? AnonymousId,
    OrderCustomerResponse Customer,
    OrderAddressResponse ShippingAddress,
    OrderAddressResponse BillingAddress,
    string Status,
    decimal TotalAmount,
    IReadOnlyList<OrderItemResponse> Items,
    string? TrackingCode,
    string? TransactionId,
    string? FailureReason,
    DateTimeOffset CreatedAtUtc = default);
