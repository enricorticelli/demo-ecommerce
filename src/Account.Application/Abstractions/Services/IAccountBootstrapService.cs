namespace Account.Application.Abstractions.Services;

public interface IAccountBootstrapService
{
    Task EnsureDefaultAdminAsync(string username, string password, CancellationToken cancellationToken);
}
