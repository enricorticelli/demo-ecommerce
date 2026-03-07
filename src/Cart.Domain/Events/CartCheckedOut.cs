namespace Cart.Domain.Events;

public sealed record CartCheckedOut(Guid CartId, Guid OrderId);