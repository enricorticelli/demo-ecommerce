using Order.Application.Views;

namespace Order.Application.Abstractions.Queries;

public interface IOrderQueryService
{
    Task<IReadOnlyList<OrderView>> ListAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<OrderView>> ListByAuthenticatedUserIdAsync(Guid authenticatedUserId, CancellationToken cancellationToken);
    Task<OrderView> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}
