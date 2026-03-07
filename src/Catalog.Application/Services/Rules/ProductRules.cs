using Catalog.Application.Abstractions.Repositories;
using Catalog.Application.Abstractions.Rules;
using Shared.BuildingBlocks.Exceptions;

namespace Catalog.Application.Services.Rules;

public sealed class ProductRules(
    IProductRepository productRepository,
    IBrandRepository brandRepository,
    ICategoryRepository categoryRepository) : IProductRules
{
    public void EnsurePriceIsPositive(decimal price)
    {
        if (price <= 0)
        {
            throw new ValidationAppException("Product price must be greater than zero.");
        }
    }

    public async Task EnsureSkuIsUniqueAsync(string sku, Guid? excludingId, CancellationToken cancellationToken)
    {
        var exists = await productRepository.ExistsBySkuAsync(sku, excludingId, cancellationToken);
        if (exists)
        {
            throw new ConflictAppException($"Product SKU '{sku}' already exists.");
        }
    }

    public async Task EnsureBrandAndCategoryExistAsync(Guid brandId, Guid categoryId, CancellationToken cancellationToken)
    {
        var brand = await brandRepository.GetByIdAsync(brandId, cancellationToken);
        if (brand is null)
        {
            throw new ValidationAppException($"Brand '{brandId}' does not exist.");
        }

        var category = await categoryRepository.GetByIdAsync(categoryId, cancellationToken);
        if (category is null)
        {
            throw new ValidationAppException($"Category '{categoryId}' does not exist.");
        }
    }
}
