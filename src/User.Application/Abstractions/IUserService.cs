namespace User.Application;

public interface IUserService
{
    Task<UserView?> GetUserByIdAsync(Guid id, CancellationToken cancellationToken);
}
