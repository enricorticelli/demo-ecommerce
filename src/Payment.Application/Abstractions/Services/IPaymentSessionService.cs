using Payment.Application.Services;
using Payment.Application.Views;

namespace Payment.Application.Abstractions.Services;

public interface IPaymentSessionService
{
    Task<IReadOnlyList<PaymentSessionView>> ListAsync(CancellationToken cancellationToken);
    Task<PaymentSessionView> GetOrCreateByOrderIdAsync(Guid orderId, string redirectUrl, CancellationToken cancellationToken);
    Task<PaymentSessionView?> GetBySessionIdAsync(Guid sessionId, CancellationToken cancellationToken);
    Task<PaymentSessionUpdateResult?> AuthorizeAsync(Guid sessionId, CancellationToken cancellationToken);
    Task<PaymentSessionUpdateResult?> RejectAsync(Guid sessionId, string? reason, CancellationToken cancellationToken);
}
