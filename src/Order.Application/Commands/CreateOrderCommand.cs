namespace Order.Application.Commands;

public sealed record CreateOrderCommand(
    Guid CartId,
    Guid UserId,
    string IdentityType,
    string PaymentMethod,
    IReadOnlyList<CreateOrderItemCommand> Items,
    decimal TotalAmount,
    Guid? AuthenticatedUserId,
    Guid? AnonymousId,
    CreateOrderCustomerCommand Customer,
    CreateOrderAddressCommand ShippingAddress,
    CreateOrderAddressCommand BillingAddress,
    string CorrelationId);
