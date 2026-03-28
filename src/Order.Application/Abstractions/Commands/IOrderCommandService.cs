using Order.Application.Commands;
using Order.Application.Views;

namespace Order.Application.Abstractions.Commands;

public interface IOrderCommandService
{
    Task<OrderView> CreateAsync(CreateOrderCommand command, CancellationToken cancellationToken);
    Task<OrderView> AdminManualCompleteAsync(ManualCompleteOrderCommand command, CancellationToken cancellationToken);
    Task<OrderView> AdminManualCancelAsync(ManualCancelOrderCommand command, CancellationToken cancellationToken);
    Task<OrderView> StoreManualCancelAsync(ManualCancelOrderCommand command, CancellationToken cancellationToken);
    Task<int> ClaimGuestOrdersAsync(ClaimGuestOrdersCommand command, CancellationToken cancellationToken);
}
