using Payment.Application;
using Shared.BuildingBlocks.Contracts;

namespace Payment.Infrastructure;

public sealed class PaymentAuthorizeRequestedHandler
{
    public static async Task Handle(PaymentAuthorizeRequestedV1 message, IPaymentService paymentService, CancellationToken cancellationToken)
    {
        await paymentService.AuthorizeAsync(message, cancellationToken);
    }
}
