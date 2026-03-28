using Payment.Application.Abstractions.Queries;
using Payment.Application.Abstractions.Services;
using Payment.Application.Views;

namespace Payment.Application.Services;

public sealed class PaymentQueryService(
    IPaymentSessionService paymentSessionService,
    IPaymentCheckoutService paymentCheckoutService) : IPaymentQueryService
{
    public Task<IReadOnlyList<PaymentSessionView>> ListAsync(CancellationToken cancellationToken)
    {
        return paymentSessionService.ListAsync(cancellationToken);
    }

    public Task<PaymentSessionView?> GetOrCreateByOrderIdAsync(Guid orderId, CancellationToken cancellationToken)
    {
        return paymentCheckoutService.EnsureCheckoutByOrderIdAsync(orderId, cancellationToken);
    }

    public Task<PaymentSessionView?> GetBySessionIdAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        return paymentSessionService.GetBySessionIdAsync(sessionId, cancellationToken);
    }
}
