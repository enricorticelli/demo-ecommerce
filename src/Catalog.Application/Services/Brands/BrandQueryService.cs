using Catalog.Application.Abstractions.Queries;
using Catalog.Application.Abstractions.Repositories;
using Catalog.Application.Views;
using Catalog.Domain.Entities;
using Shared.BuildingBlocks.Exceptions;
using Shared.BuildingBlocks.Mapping;

namespace Catalog.Application.Services.Brands;

public sealed class BrandQueryService(
    IBrandRepository brandRepository,
    IViewMapper<Brand, BrandView> mapper) : IBrandQueryService
{
    public async Task<IReadOnlyList<BrandView>> ListAsync(string? searchTerm, CancellationToken cancellationToken)
    {
        var brands = await brandRepository.ListAsync(searchTerm, cancellationToken);
        return brands.Select(mapper.Map).ToArray();
    }

    public async Task<BrandView> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var brand = await brandRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundAppException($"Brand '{id}' not found.");

        return mapper.Map(brand);
    }
}
