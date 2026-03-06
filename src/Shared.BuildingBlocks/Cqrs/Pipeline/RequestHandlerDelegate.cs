namespace Shared.BuildingBlocks.Cqrs;

public delegate Task<TResponse> RequestHandlerDelegate<TResponse>();
