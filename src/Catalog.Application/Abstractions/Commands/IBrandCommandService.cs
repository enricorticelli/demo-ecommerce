using Catalog.Application.Views;

namespace Catalog.Application.Abstractions.Commands;

public interface IBrandCommandService
{
    Task<BrandView> CreateAsync(string name, string slug, string description, string correlationId, CancellationToken cancellationToken);
    Task<BrandView> UpdateAsync(Guid id, string name, string slug, string description, string correlationId, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, string correlationId, CancellationToken cancellationToken);
}
