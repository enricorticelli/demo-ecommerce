using Cart.Application.Abstractions;
using Shared.BuildingBlocks.Contracts.Integration;

namespace Cart.Infrastructure.Messaging.Handlers;

public sealed class OrderCompletedHandler
{
    public Task Handle(OrderCompletedV1 message, ICartService cartService, CancellationToken cancellationToken)
    {
        return cartService.RotateCartAfterOrderCompletionAsync(message.CartId, message.UserId, message.OrderId, cancellationToken);
    }
}
