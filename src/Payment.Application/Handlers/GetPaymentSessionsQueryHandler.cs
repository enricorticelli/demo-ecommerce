using Shared.BuildingBlocks.Cqrs;

namespace Payment.Application;

public sealed class GetPaymentSessionsQueryHandler(IPaymentSessionService paymentSessionService)
    : IQueryHandler<GetPaymentSessionsQuery, IReadOnlyList<PaymentSessionView>>
{
    public Task<IReadOnlyList<PaymentSessionView>> HandleAsync(GetPaymentSessionsQuery query, CancellationToken cancellationToken)
    {
        return paymentSessionService.GetAllAsync(query.Limit, cancellationToken);
    }
}
