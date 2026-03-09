namespace Payment.Application.Services;

public sealed record PaymentProviderCheckoutRequest(
    Guid SessionId,
    Guid OrderId,
    Guid UserId,
    decimal Amount,
    string PaymentMethod);

