using Shared.BuildingBlocks.Cqrs;

namespace Payment.Application;

public sealed class AuthorizePaymentSessionCommandHandler(IPaymentSessionService paymentSessionService)
    : ICommandHandler<AuthorizePaymentSessionCommand, bool>
{
    public Task<bool> HandleAsync(AuthorizePaymentSessionCommand command, CancellationToken cancellationToken)
    {
        return paymentSessionService.AuthorizeSessionAsync(command.SessionId, cancellationToken);
    }
}
