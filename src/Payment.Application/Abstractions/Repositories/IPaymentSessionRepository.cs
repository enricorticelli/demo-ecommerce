using PaymentSessionEntity = Payment.Domain.Entities.PaymentSession;

namespace Payment.Application.Abstractions.Repositories;

public interface IPaymentSessionRepository
{
    Task<PaymentSessionEntity?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken);
    Task<PaymentSessionEntity?> GetBySessionIdAsync(Guid sessionId, CancellationToken cancellationToken);
    Task<IReadOnlyList<PaymentSessionEntity>> ListAsync(CancellationToken cancellationToken);
    void Add(PaymentSessionEntity session);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
