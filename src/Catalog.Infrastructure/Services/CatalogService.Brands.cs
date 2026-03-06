using Catalog.Application;
using Catalog.Domain;
using Marten;

namespace Catalog.Infrastructure;

public sealed partial class CatalogService
{
    public async Task<IReadOnlyList<BrandView>> GetBrandsAsync(CancellationToken cancellationToken)
    {
        var brands = await _querySession.Query<BrandAggregate>()
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return brands.Select(MapToView).ToArray();
    }

    public async Task<BrandView?> GetBrandByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var brand = await _querySession.LoadAsync<BrandAggregate>(id, cancellationToken);
        return brand is null ? null : MapToView(brand);
    }

    public async Task<BrandView> CreateBrandAsync(CreateBrandCommand command, CancellationToken cancellationToken)
    {
        var brand = new BrandAggregate
        {
            Id = Guid.NewGuid(),
            Name = command.Name,
            Slug = command.Slug,
            Description = command.Description
        };

        _documentSession.Store(brand);
        await _documentSession.SaveChangesAsync(cancellationToken);
        return MapToView(brand);
    }

    public async Task<BrandView?> UpdateBrandAsync(Guid id, UpdateBrandCommand command, CancellationToken cancellationToken)
    {
        var existing = await _documentSession.LoadAsync<BrandAggregate>(id, cancellationToken);
        if (existing is null)
        {
            return null;
        }

        var updated = new BrandAggregate
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

    public async Task<bool> DeleteBrandAsync(Guid id, CancellationToken cancellationToken)
    {
        var brand = await _documentSession.LoadAsync<BrandAggregate>(id, cancellationToken);
        if (brand is null)
        {
            return false;
        }

        _documentSession.Delete<BrandAggregate>(id);
        await _documentSession.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static BrandView MapToView(BrandAggregate brand)
    {
        return new BrandView(brand.Id, brand.Name, brand.Slug, brand.Description);
    }
}
