using Shared.BuildingBlocks.Cqrs;

namespace Payment.Application;

public sealed record GetPaymentSessionByOrderIdQuery(Guid OrderId) : IQuery<PaymentSessionView?>;
