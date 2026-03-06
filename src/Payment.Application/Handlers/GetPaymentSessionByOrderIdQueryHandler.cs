using Shared.BuildingBlocks.Cqrs;

namespace Payment.Application;

public sealed class GetPaymentSessionByOrderIdQueryHandler(IPaymentSessionService paymentSessionService)
    : IQueryHandler<GetPaymentSessionByOrderIdQuery, PaymentSessionView?>
{
    public Task<PaymentSessionView?> HandleAsync(GetPaymentSessionByOrderIdQuery query, CancellationToken cancellationToken)
    {
        return paymentSessionService.GetByOrderIdAsync(query.OrderId, cancellationToken);
    }
}
