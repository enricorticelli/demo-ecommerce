namespace Shared.BuildingBlocks.ReadModels;

public interface IReadModelStore<TKey, TReadModel>
    where TKey : notnull
    where TReadModel : class
{
    Task<TReadModel?> GetAsync(TKey id, CancellationToken cancellationToken);
    Task UpsertAsync(TReadModel model, CancellationToken cancellationToken);
}
