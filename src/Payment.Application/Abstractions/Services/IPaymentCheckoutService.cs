using Payment.Application.Views;

namespace Payment.Application.Abstractions.Services;

public interface IPaymentCheckoutService
{
    Task<PaymentSessionView?> EnsureCheckoutByOrderIdAsync(Guid orderId, CancellationToken cancellationToken);
}

