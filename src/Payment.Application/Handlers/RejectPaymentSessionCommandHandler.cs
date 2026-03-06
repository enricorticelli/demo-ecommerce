using Shared.BuildingBlocks.Cqrs;

namespace Payment.Application;

public sealed class RejectPaymentSessionCommandHandler(IPaymentSessionService paymentSessionService)
    : ICommandHandler<RejectPaymentSessionCommand, bool>
{
    public Task<bool> HandleAsync(RejectPaymentSessionCommand command, CancellationToken cancellationToken)
    {
        return paymentSessionService.RejectSessionAsync(command.SessionId, command.Reason, cancellationToken);
    }
}
