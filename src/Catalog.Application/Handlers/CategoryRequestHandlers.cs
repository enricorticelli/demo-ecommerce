using Shared.BuildingBlocks.Cqrs;

namespace Catalog.Application;

public sealed class CategoryRequestHandlers(ICatalogService catalogService) :
    IQueryHandler<GetCategoriesQuery, IReadOnlyList<CategoryView>>,
    IQueryHandler<GetCategoryByIdQuery, CategoryView?>,
    ICommandHandler<CreateCategoryCatalogCommand, CategoryView>,
    ICommandHandler<UpdateCategoryCatalogCommand, CategoryView?>,
    ICommandHandler<DeleteCategoryCatalogCommand, bool>
{
    public Task<IReadOnlyList<CategoryView>> HandleAsync(GetCategoriesQuery query, CancellationToken cancellationToken)
    {
        return catalogService.GetCategoriesAsync(cancellationToken);
    }

    public Task<CategoryView?> HandleAsync(GetCategoryByIdQuery query, CancellationToken cancellationToken)
    {
        return catalogService.GetCategoryByIdAsync(query.CategoryId, cancellationToken);
    }

    public Task<CategoryView> HandleAsync(CreateCategoryCatalogCommand command, CancellationToken cancellationToken)
    {
        return catalogService.CreateCategoryAsync(command.Category, cancellationToken);
    }

    public Task<CategoryView?> HandleAsync(UpdateCategoryCatalogCommand command, CancellationToken cancellationToken)
    {
        return catalogService.UpdateCategoryAsync(command.CategoryId, command.Category, cancellationToken);
    }

    public Task<bool> HandleAsync(DeleteCategoryCatalogCommand command, CancellationToken cancellationToken)
    {
        return catalogService.DeleteCategoryAsync(command.CategoryId, cancellationToken);
    }
}
