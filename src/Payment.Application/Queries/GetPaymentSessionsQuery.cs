using Shared.BuildingBlocks.Cqrs;

namespace Payment.Application;

public sealed record GetPaymentSessionsQuery(int Limit) : IQuery<IReadOnlyList<PaymentSessionView>>;
