using Payment.Application.Abstractions.Repositories;
using Payment.Application.Abstractions.Services;
using Payment.Application.Views;
using System.Text.RegularExpressions;

namespace Payment.Application.Services;

public sealed class PaymentSessionService(IPaymentSessionRepository paymentSessionRepository) : IPaymentSessionService
{
    public async Task<IReadOnlyList<PaymentSessionView>> ListAsync(CancellationToken cancellationToken)
    {
        var sessions = await paymentSessionRepository.ListAsync(cancellationToken);
        return sessions
            .Select(ToView)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToArray();
    }

    public async Task<PaymentSessionView?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var session = await paymentSessionRepository.GetByOrderIdAsync(orderId, cancellationToken);
        return session is null ? null : ToView(session);
    }

    public async Task<PaymentSessionView?> GetBySessionIdAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var session = await paymentSessionRepository.GetBySessionIdAsync(sessionId, cancellationToken);
        return session is null ? null : ToView(session);
    }

    public async Task<PaymentSessionUpdateResult?> AuthorizeAsync(Guid sessionId, string? transactionId, CancellationToken cancellationToken)
    {
        var session = await paymentSessionRepository.GetBySessionIdAsync(sessionId, cancellationToken);
        if (session is null)
        {
            return null;
        }

        var effectiveTransactionId = string.IsNullOrWhiteSpace(transactionId)
            ? $"TX-{Guid.NewGuid():N}"
            : transactionId.Trim();

        var statusChanged = session.Authorize(effectiveTransactionId);
        if (statusChanged)
        {
            await paymentSessionRepository.SaveChangesAsync(cancellationToken);
        }

        return new PaymentSessionUpdateResult(ToView(session), statusChanged);
    }

    public async Task<PaymentSessionUpdateResult?> RejectAsync(Guid sessionId, string? reason, CancellationToken cancellationToken)
    {
        var session = await paymentSessionRepository.GetBySessionIdAsync(sessionId, cancellationToken);
        if (session is null)
        {
            return null;
        }

        var statusChanged = session.Reject(SanitizeFailureReason(reason));
        if (statusChanged)
        {
            await paymentSessionRepository.SaveChangesAsync(cancellationToken);
        }

        return new PaymentSessionUpdateResult(ToView(session), statusChanged);
    }

    private static PaymentSessionView ToView(Domain.Entities.PaymentSession session)
    {
        return new PaymentSessionView(
            session.SessionId,
            session.OrderId,
            session.UserId,
            session.Amount,
            session.PaymentMethod,
            session.ProviderCode,
            string.IsNullOrWhiteSpace(session.ExternalCheckoutId) ? null : session.ExternalCheckoutId,
            session.ProviderStatus,
            session.Status,
            session.TransactionId,
            session.FailureReason,
            session.CreatedAtUtc,
            session.CompletedAtUtc,
            session.RedirectUrl);
    }

    private static string SanitizeFailureReason(string? reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return "Payment rejected.";
        }

        var sanitized = Regex.Replace(reason.Trim(), "\\b\\d{12,19}\\b", "[REDACTED]");
        if (sanitized.Length > 256)
        {
            sanitized = sanitized[..256];
        }

        return sanitized;
    }

}
