namespace Shared.BuildingBlocks.Cqrs;

public interface ICommand<TResult> : IRequest<TResult>;
