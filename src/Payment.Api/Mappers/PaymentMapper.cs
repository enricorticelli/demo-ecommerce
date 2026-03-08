using Payment.Api.Contracts.Responses;
using Payment.Application.Views;

namespace Payment.Api.Mappers;

public static class PaymentMapper
{
    public static PaymentSessionResponse ToResponse(PaymentSessionView session)
    {
        return new PaymentSessionResponse(
            session.SessionId,
            session.OrderId,
            session.UserId,
            session.Amount,
            session.PaymentMethod,
            session.Status,
            session.TransactionId,
            session.FailureReason,
            session.CreatedAtUtc,
            session.CompletedAtUtc,
            session.RedirectUrl);
    }
}
