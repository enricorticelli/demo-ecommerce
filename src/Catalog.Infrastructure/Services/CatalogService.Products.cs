using Catalog.Application;
using Catalog.Domain;
using Marten;

namespace Catalog.Infrastructure;

public sealed partial class CatalogService
{
    public async Task<IReadOnlyList<ProductView>> GetProductsAsync(CancellationToken cancellationToken)
    {
        var products = await _querySession.Query<ProductAggregate>()
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);

        return await BuildProductViewsAsync(products, cancellationToken);
    }

    public async Task<IReadOnlyList<ProductView>> GetNewArrivalsAsync(CancellationToken cancellationToken)
    {
        var products = await _querySession.Query<ProductAggregate>()
            .Where(p => p.IsNewArrival)
            .OrderByDescending(p => p.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return await BuildProductViewsAsync(products, cancellationToken);
    }

    public async Task<IReadOnlyList<ProductView>> GetBestSellersAsync(CancellationToken cancellationToken)
    {
        var products = await _querySession.Query<ProductAggregate>()
            .Where(p => p.IsBestSeller)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);

        return await BuildProductViewsAsync(products, cancellationToken);
    }

    public async Task<ProductView?> GetProductByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var product = await _querySession.LoadAsync<ProductAggregate>(id, cancellationToken);
        if (product is null)
        {
            return null;
        }

        return await BuildProductViewAsync(product, cancellationToken);
    }

    public async Task<ProductView?> CreateProductAsync(CreateProductCommand command, CancellationToken cancellationToken)
    {
        var isValidReference = await ReferencesExistAsync(command.BrandId, command.CategoryId, command.CollectionIds, cancellationToken);
        if (!isValidReference)
        {
            return null;
        }

        var product = new ProductAggregate
        {
            Id = Guid.NewGuid(),
            Sku = command.Sku,
            Name = command.Name,
            Description = command.Description,
            Price = command.Price,
            BrandId = command.BrandId,
            CategoryId = command.CategoryId,
            CollectionIds = command.CollectionIds.Distinct().ToArray(),
            IsNewArrival = command.IsNewArrival,
            IsBestSeller = command.IsBestSeller,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        _documentSession.Store(product);
        await _documentSession.SaveChangesAsync(cancellationToken);

        return await BuildProductViewAsync(product, cancellationToken);
    }

    public async Task<ProductView?> UpdateProductAsync(Guid id, UpdateProductCommand command, CancellationToken cancellationToken)
    {
        var existing = await _documentSession.LoadAsync<ProductAggregate>(id, cancellationToken);
        if (existing is null)
        {
            return null;
        }

        var isValidReference = await ReferencesExistAsync(command.BrandId, command.CategoryId, command.CollectionIds, cancellationToken);
        if (!isValidReference)
        {
            return null;
        }

        var updated = new ProductAggregate
        {
            Id = id,
            Sku = command.Sku,
            Name = command.Name,
            Description = command.Description,
            Price = command.Price,
            BrandId = command.BrandId,
            CategoryId = command.CategoryId,
            CollectionIds = command.CollectionIds.Distinct().ToArray(),
            IsNewArrival = command.IsNewArrival,
            IsBestSeller = command.IsBestSeller,
            CreatedAtUtc = existing.CreatedAtUtc
        };

        _documentSession.Store(updated);
        await _documentSession.SaveChangesAsync(cancellationToken);

        return await BuildProductViewAsync(updated, cancellationToken);
    }

    public async Task<bool> DeleteProductAsync(Guid id, CancellationToken cancellationToken)
    {
        var existing = await _documentSession.LoadAsync<ProductAggregate>(id, cancellationToken);
        if (existing is null)
        {
            return false;
        }

        _documentSession.Delete<ProductAggregate>(id);
        await _documentSession.SaveChangesAsync(cancellationToken);
        return true;
    }
}
