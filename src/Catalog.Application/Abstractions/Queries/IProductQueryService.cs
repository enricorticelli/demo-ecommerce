using Catalog.Application.Views;

namespace Catalog.Application.Abstractions.Queries;

public interface IProductQueryService
{
    Task<IReadOnlyList<ProductView>> ListAsync(string? searchTerm, CancellationToken cancellationToken);
    Task<IReadOnlyList<ProductView>> ListNewArrivalsAsync(string? searchTerm, CancellationToken cancellationToken);
    Task<IReadOnlyList<ProductView>> ListBestSellersAsync(string? searchTerm, CancellationToken cancellationToken);
    Task<ProductView> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}
