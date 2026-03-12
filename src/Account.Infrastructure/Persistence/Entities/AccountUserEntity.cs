namespace Account.Infrastructure.Persistence.Entities;

public sealed class AccountUserEntity
{
    public Guid Id { get; set; }
    public string Realm { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string NormalizedEmail { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool IsEmailVerified { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public bool IsSuperUser { get; set; }
    public string[]? CustomPermissions { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}
