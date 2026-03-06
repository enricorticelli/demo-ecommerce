using Shared.BuildingBlocks.Cqrs;

namespace User.Application;

public sealed record GetUserByIdQuery(Guid UserId) : IQuery<UserView?>;
