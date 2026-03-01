using Shared.BuildingBlocks.Contracts;
using Wolverine;

namespace Payment.Api.Handlers;

public class PaymentHandlers
{
    public static async Task Handle(PaymentAuthorizeRequestedV1 message, IMessageBus bus)
    {
        if (message.Amount <= 0 || message.Amount > 10000)
        {
            await bus.PublishAsync(new PaymentFailedV1(message.OrderId, "Payment declined"));
            return;
        }

        var transactionId = $"TX-{Guid.NewGuid():N}";
        await bus.PublishAsync(new PaymentAuthorizedV1(message.OrderId, transactionId));
    }
}
