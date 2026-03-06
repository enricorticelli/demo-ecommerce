using Shared.BuildingBlocks.Cqrs;

namespace Payment.Application;

public sealed record GetPaymentSessionByIdQuery(Guid SessionId) : IQuery<PaymentSessionView?>;
