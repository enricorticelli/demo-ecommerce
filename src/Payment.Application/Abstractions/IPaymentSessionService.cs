namespace Payment.Application;

public interface IPaymentSessionService
{
    Task<PaymentSessionView?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken);
    Task<PaymentSessionView?> GetBySessionIdAsync(Guid sessionId, CancellationToken cancellationToken);
    Task<bool> AuthorizeSessionAsync(Guid sessionId, CancellationToken cancellationToken);
    Task<bool> RejectSessionAsync(Guid sessionId, string reason, CancellationToken cancellationToken);
}
