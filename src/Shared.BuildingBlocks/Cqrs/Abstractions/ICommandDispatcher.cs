namespace Shared.BuildingBlocks.Cqrs;

public interface ICommandDispatcher
{
    Task<TResult> ExecuteAsync<TResult>(ICommand<TResult> command, CancellationToken cancellationToken);
}
