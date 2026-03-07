using Catalog.Application.Abstractions.Queries;
using Catalog.Application.Abstractions.Repositories;
using Catalog.Application.Views;
using Catalog.Domain.Entities;
using Shared.BuildingBlocks.Exceptions;
using Shared.BuildingBlocks.Mapping;

namespace Catalog.Application.Services.Products;

public sealed class ProductQueryService(
    IProductRepository productRepository,
    IViewMapper<Product, ProductView> mapper) : IProductQueryService
{
    public async Task<IReadOnlyList<ProductView>> ListAsync(string? searchTerm, CancellationToken cancellationToken)
    {
        var products = await productRepository.ListAsync(searchTerm, cancellationToken);
        return products.Select(mapper.Map).ToArray();
    }

    public async Task<IReadOnlyList<ProductView>> ListNewArrivalsAsync(string? searchTerm, CancellationToken cancellationToken)
    {
        var products = await productRepository.ListNewArrivalsAsync(searchTerm, cancellationToken);
        return products.Select(mapper.Map).ToArray();
    }

    public async Task<IReadOnlyList<ProductView>> ListBestSellersAsync(string? searchTerm, CancellationToken cancellationToken)
    {
        var products = await productRepository.ListBestSellersAsync(searchTerm, cancellationToken);
        return products.Select(mapper.Map).ToArray();
    }

    public async Task<ProductView> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var product = await productRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundAppException($"Product '{id}' not found.");

        return mapper.Map(product);
    }
}
