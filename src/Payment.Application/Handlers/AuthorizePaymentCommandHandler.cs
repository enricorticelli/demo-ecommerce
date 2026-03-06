using Shared.BuildingBlocks.Cqrs;

namespace Payment.Application;

public sealed class AuthorizePaymentCommandHandler(IPaymentService paymentService)
    : ICommandHandler<AuthorizePaymentCommand, PaymentAuthorizationResult>
{
    public Task<PaymentAuthorizationResult> HandleAsync(AuthorizePaymentCommand command, CancellationToken cancellationToken)
    {
        return paymentService.AuthorizeAsync(command.Request, cancellationToken);
    }
}
