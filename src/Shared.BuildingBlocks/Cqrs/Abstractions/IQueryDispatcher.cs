namespace Shared.BuildingBlocks.Cqrs;

public interface IQueryDispatcher
{
    Task<TResult> ExecuteAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken);
}
