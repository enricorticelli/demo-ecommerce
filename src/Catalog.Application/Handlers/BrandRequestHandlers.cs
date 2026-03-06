using Shared.BuildingBlocks.Cqrs;

namespace Catalog.Application;

public sealed class BrandRequestHandlers(ICatalogService catalogService) :
    IQueryHandler<GetBrandsQuery, IReadOnlyList<BrandView>>,
    IQueryHandler<GetBrandByIdQuery, BrandView?>,
    ICommandHandler<CreateBrandCatalogCommand, BrandView>,
    ICommandHandler<UpdateBrandCatalogCommand, BrandView?>,
    ICommandHandler<DeleteBrandCatalogCommand, bool>
{
    public Task<IReadOnlyList<BrandView>> HandleAsync(GetBrandsQuery query, CancellationToken cancellationToken)
    {
        return catalogService.GetBrandsAsync(cancellationToken);
    }

    public Task<BrandView?> HandleAsync(GetBrandByIdQuery query, CancellationToken cancellationToken)
    {
        return catalogService.GetBrandByIdAsync(query.BrandId, cancellationToken);
    }

    public Task<BrandView> HandleAsync(CreateBrandCatalogCommand command, CancellationToken cancellationToken)
    {
        return catalogService.CreateBrandAsync(command.Brand, cancellationToken);
    }

    public Task<BrandView?> HandleAsync(UpdateBrandCatalogCommand command, CancellationToken cancellationToken)
    {
        return catalogService.UpdateBrandAsync(command.BrandId, command.Brand, cancellationToken);
    }

    public Task<bool> HandleAsync(DeleteBrandCatalogCommand command, CancellationToken cancellationToken)
    {
        return catalogService.DeleteBrandAsync(command.BrandId, cancellationToken);
    }
}
