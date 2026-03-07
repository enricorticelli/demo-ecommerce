using Catalog.Application.Abstractions.Commands;
using Catalog.Application.Abstractions.Repositories;
using Catalog.Application.Abstractions.Rules;
using Catalog.Application.Views;
using Catalog.Domain.Entities;
using Shared.BuildingBlocks.Contracts.IntegrationEvents;
using Shared.BuildingBlocks.Contracts.IntegrationEvents.Catalog;
using Shared.BuildingBlocks.Contracts.Messaging;
using Shared.BuildingBlocks.Exceptions;
using Shared.BuildingBlocks.Mapping;

namespace Catalog.Application.Services.Products;

public sealed class ProductCommandCatalogService(
    IProductRepository productRepository,
    IProductRules productRules,
    ICollectionRules collectionRules,
    IDomainEventPublisher eventPublisher,
    IViewMapper<Product, ProductView> mapper) : IProductCommandCatalogService
{
    public async Task<ProductView> CreateAsync(string sku, string name, string description, decimal price, Guid brandId, Guid categoryId,
        IReadOnlyList<Guid> collectionIds, bool isNewArrival, bool isBestSeller, string correlationId,
        CancellationToken cancellationToken)
    {
        var normalizedSku = sku.Trim();
        var normalizedName = name.Trim();
        var normalizedDescription = description.Trim();
        var normalizedCollectionIds = collectionIds.Distinct().ToArray();

        productRules.EnsurePriceIsPositive(price);
        await productRules.EnsureSkuIsUniqueAsync(normalizedSku, null, cancellationToken);
        await productRules.EnsureBrandAndCategoryExistAsync(brandId, categoryId, cancellationToken);
        await collectionRules.EnsureCollectionsExistAsync(normalizedCollectionIds, cancellationToken);

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Sku = normalizedSku,
            Name = normalizedName,
            Description = normalizedDescription,
            Price = price,
            BrandId = brandId,
            CategoryId = categoryId,
            IsNewArrival = isNewArrival,
            IsBestSeller = isBestSeller,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        foreach (var collectionId in normalizedCollectionIds)
        {
            product.ProductCollections.Add(new ProductCollection { ProductId = product.Id, CollectionId = collectionId });
        }

        productRepository.Add(product);

        var integrationEvent = new ProductCreatedV1(product.Id, product.Sku, product.BrandId, product.CategoryId, normalizedCollectionIds, CreateMetadata(correlationId));
        await eventPublisher.PublishAndFlushAsync(integrationEvent, cancellationToken);

        var persistedProduct = await productRepository.GetByIdAsync(product.Id, cancellationToken)
            ?? throw new NotFoundAppException($"Product '{product.Id}' not found.");

        return mapper.Map(persistedProduct);
    }

    public async Task<ProductView> UpdateAsync(Guid id, string sku, string name, string description, decimal price, Guid brandId,
        Guid categoryId, IReadOnlyList<Guid> collectionIds, bool isNewArrival, bool isBestSeller,
        string correlationId, CancellationToken cancellationToken)
    {
        var product = await productRepository.GetByIdWithCollectionsAsync(id, cancellationToken)
            ?? throw new NotFoundAppException($"Product '{id}' not found.");

        var normalizedSku = sku.Trim();
        var normalizedName = name.Trim();
        var normalizedDescription = description.Trim();
        var normalizedCollectionIds = collectionIds.Distinct().ToArray();

        productRules.EnsurePriceIsPositive(price);
        await productRules.EnsureSkuIsUniqueAsync(normalizedSku, id, cancellationToken);
        await productRules.EnsureBrandAndCategoryExistAsync(brandId, categoryId, cancellationToken);
        await collectionRules.EnsureCollectionsExistAsync(normalizedCollectionIds, cancellationToken);

        product.Sku = normalizedSku;
        product.Name = normalizedName;
        product.Description = normalizedDescription;
        product.Price = price;
        product.BrandId = brandId;
        product.CategoryId = categoryId;
        product.IsNewArrival = isNewArrival;
        product.IsBestSeller = isBestSeller;

        productRepository.ReplaceCollections(product, normalizedCollectionIds);

        var integrationEvent = new ProductUpdatedV1(product.Id, product.Sku, product.BrandId, product.CategoryId, normalizedCollectionIds, CreateMetadata(correlationId));
        await eventPublisher.PublishAndFlushAsync(integrationEvent, cancellationToken);

        var persistedProduct = await productRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundAppException($"Product '{id}' not found.");

        return mapper.Map(persistedProduct);
    }

    public async Task DeleteAsync(Guid id, string correlationId, CancellationToken cancellationToken)
    {
        var product = await productRepository.GetByIdWithCollectionsAsync(id, cancellationToken)
            ?? throw new NotFoundAppException($"Product '{id}' not found.");

        productRepository.Remove(product);

        var integrationEvent = new ProductDeletedV1(id, CreateMetadata(correlationId));
        await eventPublisher.PublishAndFlushAsync(integrationEvent, cancellationToken);
    }

    private static IntegrationEventMetadata CreateMetadata(string correlationId)
    {
        return new IntegrationEventMetadata(Guid.NewGuid(), DateTimeOffset.UtcNow, correlationId, "Catalog");
    }
}
