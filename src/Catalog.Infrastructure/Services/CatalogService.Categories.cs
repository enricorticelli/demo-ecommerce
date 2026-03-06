using Catalog.Application;
using Catalog.Domain;
using Marten;

namespace Catalog.Infrastructure;

public sealed partial class CatalogService
{
    public async Task<IReadOnlyList<CategoryView>> GetCategoriesAsync(CancellationToken cancellationToken)
    {
        var categories = await _querySession.Query<CategoryAggregate>()
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return categories.Select(MapToView).ToArray();
    }

    public async Task<CategoryView?> GetCategoryByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var category = await _querySession.LoadAsync<CategoryAggregate>(id, cancellationToken);
        return category is null ? null : MapToView(category);
    }

    public async Task<CategoryView> CreateCategoryAsync(CreateCategoryCommand command, CancellationToken cancellationToken)
    {
        var category = new CategoryAggregate
        {
            Id = Guid.NewGuid(),
            Name = command.Name,
            Slug = command.Slug,
            Description = command.Description
        };

        _documentSession.Store(category);
        await _documentSession.SaveChangesAsync(cancellationToken);
        return MapToView(category);
    }

    public async Task<CategoryView?> UpdateCategoryAsync(Guid id, UpdateCategoryCommand command, CancellationToken cancellationToken)
    {
        var existing = await _documentSession.LoadAsync<CategoryAggregate>(id, cancellationToken);
        if (existing is null)
        {
            return null;
        }

        var updated = new CategoryAggregate
        {
            Id = id,
            Name = command.Name,
            Slug = command.Slug,
            Description = command.Description
        };

        _documentSession.Store(updated);
        await _documentSession.SaveChangesAsync(cancellationToken);
        return MapToView(updated);
    }

    public async Task<bool> DeleteCategoryAsync(Guid id, CancellationToken cancellationToken)
    {
        var category = await _documentSession.LoadAsync<CategoryAggregate>(id, cancellationToken);
        if (category is null)
        {
            return false;
        }

        _documentSession.Delete<CategoryAggregate>(id);
        await _documentSession.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static CategoryView MapToView(CategoryAggregate category)
    {
        return new CategoryView(category.Id, category.Name, category.Slug, category.Description);
    }
}
