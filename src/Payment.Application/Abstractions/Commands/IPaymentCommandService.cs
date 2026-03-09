using Payment.Application.Services;

namespace Payment.Application.Abstractions.Commands;

public interface IPaymentCommandService
{
    Task<PaymentSessionUpdateResult?> AuthorizeAsync(Guid sessionId, string correlationId, string? transactionId, CancellationToken cancellationToken);
    Task<PaymentSessionUpdateResult?> RejectAsync(Guid sessionId, string? reason, string correlationId, CancellationToken cancellationToken);
}
