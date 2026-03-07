namespace Catalog.Application.Abstractions.Rules;

public interface IProductRules
{
    void EnsurePriceIsPositive(decimal price);
    Task EnsureSkuIsUniqueAsync(string sku, Guid? excludingId, CancellationToken cancellationToken);
    Task EnsureBrandAndCategoryExistAsync(Guid brandId, Guid categoryId, CancellationToken cancellationToken);
}
