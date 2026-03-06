using Microsoft.Extensions.DependencyInjection;

namespace Shared.BuildingBlocks.Cqrs;

public sealed class CqrsDispatcher(IServiceProvider serviceProvider) : ICommandDispatcher, IQueryDispatcher
{
    public Task<TResult> ExecuteAsync<TResult>(ICommand<TResult> command, CancellationToken cancellationToken)
    {
        return ExecuteInternalAsync(command, typeof(ICommandHandler<,>), cancellationToken);
    }

    public Task<TResult> ExecuteAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken)
    {
        return ExecuteInternalAsync(query, typeof(IQueryHandler<,>), cancellationToken);
    }

    private Task<TResult> ExecuteInternalAsync<TResult>(IRequest<TResult> request, Type handlerGenericType, CancellationToken cancellationToken)
    {
        var requestType = request.GetType();
        var handlerType = handlerGenericType.MakeGenericType(requestType, typeof(TResult));
        var handler = serviceProvider.GetRequiredService(handlerType);

        var pipelineType = typeof(IEnumerable<>).MakeGenericType(typeof(IPipelineBehavior<,>).MakeGenericType(requestType, typeof(TResult)));
        var resolvedBehaviors = serviceProvider.GetService(pipelineType) as IEnumerable<object> ?? Array.Empty<object>();
        var behaviors = resolvedBehaviors.Reverse().ToArray();

        RequestHandlerDelegate<TResult> next = () => (Task<TResult>)handlerType
            .GetMethod("HandleAsync")!
            .Invoke(handler, [request, cancellationToken])!;

        foreach (var behavior in behaviors)
        {
            var currentNext = next;
            next = () => (Task<TResult>)behavior.GetType()
                .GetMethod("HandleAsync")!
                .Invoke(behavior, [request, cancellationToken, currentNext])!;
        }

        return next();
    }
}
