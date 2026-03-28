using OrderEntity = Order.Domain.Entities.Order;

namespace Order.Application.Abstractions.Repositories;

public interface IOrderRepository
{
    Task<IReadOnlyList<OrderEntity>> ListAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<OrderEntity>> ListByAuthenticatedUserIdAsync(Guid authenticatedUserId, CancellationToken cancellationToken);
    Task<IReadOnlyList<OrderEntity>> ListAnonymousByCustomerEmailAsync(string customerEmail, CancellationToken cancellationToken);
    Task<OrderEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    void Add(OrderEntity order);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
