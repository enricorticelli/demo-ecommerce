using Payment.Application.Abstractions.Providers;
using Payment.Application.Abstractions.Repositories;
using Payment.Application.Abstractions.Services;
using Payment.Application.Views;

namespace Payment.Application.Services;

public sealed class PaymentCheckoutService(
    IPaymentSessionRepository paymentSessionRepository,
    IPaymentProviderRouter paymentProviderRouter) : IPaymentCheckoutService
{
    public async Task<PaymentSessionView?> EnsureCheckoutByOrderIdAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var session = await paymentSessionRepository.GetByOrderIdAsync(orderId, cancellationToken);
        if (session is null)
        {
            return null;
        }

        if (!string.Equals(session.Status, "Pending", StringComparison.OrdinalIgnoreCase))
        {
            return ToView(session);
        }

        if (!string.IsNullOrWhiteSpace(session.RedirectUrl)
            && !string.IsNullOrWhiteSpace(session.ExternalCheckoutId)
            && !string.IsNullOrWhiteSpace(session.ProviderCode))
        {
            return ToView(session);
        }

        var providerClient = paymentProviderRouter.ResolveByPaymentMethod(session.PaymentMethod);
        var providerCheckout = await providerClient.CreateCheckoutAsync(
            new PaymentProviderCheckoutRequest(
                session.SessionId,
                session.OrderId,
                session.UserId,
                session.Amount,
                session.PaymentMethod),
            cancellationToken);

        if (session.ConfigureProviderCheckout(
                providerCheckout.ProviderCode,
                providerCheckout.ExternalCheckoutId,
                providerCheckout.RedirectUrl,
                providerCheckout.ProviderStatus))
        {
            await paymentSessionRepository.SaveChangesAsync(cancellationToken);
        }

        return ToView(session);
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
}

