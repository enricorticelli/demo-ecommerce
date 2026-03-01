namespace Shared.BuildingBlocks.Contracts;

public sealed record OrderItemDto(Guid ProductId, string Sku, string Name, int Quantity, decimal UnitPrice);

public sealed record CartCheckedOutV1(Guid CartId, Guid OrderId, Guid UserId, IReadOnlyList<OrderItemDto> Items, decimal TotalAmount);
public sealed record OrderPlacedV1(Guid OrderId, Guid UserId, IReadOnlyList<OrderItemDto> Items, decimal TotalAmount);
public sealed record StockReserveRequestedV1(Guid OrderId, IReadOnlyList<OrderItemDto> Items);
public sealed record StockReservedV1(Guid OrderId);
public sealed record StockRejectedV1(Guid OrderId, string Reason);
public sealed record PaymentAuthorizeRequestedV1(Guid OrderId, Guid UserId, decimal Amount);
public sealed record PaymentAuthorizedV1(Guid OrderId, string TransactionId);
public sealed record PaymentFailedV1(Guid OrderId, string Reason);
public sealed record ShippingCreateRequestedV1(Guid OrderId, Guid UserId, IReadOnlyList<OrderItemDto> Items);
public sealed record ShippingCreatedV1(Guid OrderId, string TrackingCode);
public sealed record OrderCompletedV1(Guid OrderId, string TrackingCode, string TransactionId);
public sealed record OrderFailedV1(Guid OrderId, string Reason);
