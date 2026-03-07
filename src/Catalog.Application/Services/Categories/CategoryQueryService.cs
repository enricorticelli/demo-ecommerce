using Catalog.Application.Abstractions.Queries;
using Catalog.Application.Abstractions.Repositories;
using Catalog.Application.Views;
using Catalog.Domain.Entities;
using Shared.BuildingBlocks.Exceptions;
using Shared.BuildingBlocks.Mapping;

namespace Catalog.Application.Services.Categories;

public sealed class CategoryQueryService(
    ICategoryRepository categoryRepository,
    IViewMapper<Category, CategoryView> mapper) : ICategoryQueryService
{
    public async Task<IReadOnlyList<CategoryView>> ListAsync(string? searchTerm, CancellationToken cancellationToken)
    {
        var categories = await categoryRepository.ListAsync(searchTerm, cancellationToken);
        return categories.Select(mapper.Map).ToArray();
    }

    public async Task<CategoryView> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var category = await categoryRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundAppException($"Category '{id}' not found.");

        return mapper.Map(category);
    }
}
