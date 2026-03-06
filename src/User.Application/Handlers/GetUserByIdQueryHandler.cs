using Shared.BuildingBlocks.Cqrs;

namespace User.Application;

public sealed class GetUserByIdQueryHandler(IUserService userService)
    : IQueryHandler<GetUserByIdQuery, UserView?>
{
    public Task<UserView?> HandleAsync(GetUserByIdQuery query, CancellationToken cancellationToken)
    {
        return userService.GetUserByIdAsync(query.UserId, cancellationToken);
    }
}
