using Catalog.Application.Views;

namespace Catalog.Application.Abstractions.Commands;

public interface ICategoryCommandService
{
    Task<CategoryView> CreateAsync(string name, string slug, string description, string correlationId, CancellationToken cancellationToken);
    Task<CategoryView> UpdateAsync(Guid id, string name, string slug, string description, string correlationId, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, string correlationId, CancellationToken cancellationToken);
}
