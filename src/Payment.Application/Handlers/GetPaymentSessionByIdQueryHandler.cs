using Shared.BuildingBlocks.Cqrs;

namespace Payment.Application;

public sealed class GetPaymentSessionByIdQueryHandler(IPaymentSessionService paymentSessionService)
    : IQueryHandler<GetPaymentSessionByIdQuery, PaymentSessionView?>
{
    public Task<PaymentSessionView?> HandleAsync(GetPaymentSessionByIdQuery query, CancellationToken cancellationToken)
    {
        return paymentSessionService.GetBySessionIdAsync(query.SessionId, cancellationToken);
    }
}
