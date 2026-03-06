namespace Catalog.Application;

public interface ICatalogService
{
    Task<IReadOnlyList<ProductView>> GetProductsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<ProductView>> GetNewArrivalsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<ProductView>> GetBestSellersAsync(CancellationToken cancellationToken);
    Task<ProductView?> GetProductByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<ProductView?> CreateProductAsync(CreateProductCommand command, CancellationToken cancellationToken);
    Task<ProductView?> UpdateProductAsync(Guid id, UpdateProductCommand command, CancellationToken cancellationToken);
    Task<bool> DeleteProductAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<BrandView>> GetBrandsAsync(CancellationToken cancellationToken);
    Task<BrandView?> GetBrandByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<BrandView> CreateBrandAsync(CreateBrandCommand command, CancellationToken cancellationToken);
    Task<BrandView?> UpdateBrandAsync(Guid id, UpdateBrandCommand command, CancellationToken cancellationToken);
    Task<bool> DeleteBrandAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<CategoryView>> GetCategoriesAsync(CancellationToken cancellationToken);
    Task<CategoryView?> GetCategoryByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<CategoryView> CreateCategoryAsync(CreateCategoryCommand command, CancellationToken cancellationToken);
    Task<CategoryView?> UpdateCategoryAsync(Guid id, UpdateCategoryCommand command, CancellationToken cancellationToken);
    Task<bool> DeleteCategoryAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<CollectionView>> GetCollectionsAsync(CancellationToken cancellationToken);
    Task<CollectionView?> GetCollectionByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<CollectionView> CreateCollectionAsync(CreateCollectionCommand command, CancellationToken cancellationToken);
    Task<CollectionView?> UpdateCollectionAsync(Guid id, UpdateCollectionCommand command, CancellationToken cancellationToken);
    Task<bool> DeleteCollectionAsync(Guid id, CancellationToken cancellationToken);
}
