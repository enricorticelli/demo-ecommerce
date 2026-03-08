using Shipping.Application.Views;

namespace Shipping.Application.Abstractions.Queries;

public interface IShippingQueryService
{
    Task<IReadOnlyList<ShipmentView>> ListAsync(int limit, int offset, string? searchTerm, CancellationToken cancellationToken);
    Task<ShipmentView> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken);
}
