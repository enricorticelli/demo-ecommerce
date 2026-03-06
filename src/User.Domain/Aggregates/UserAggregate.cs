namespace User.Domain;

public sealed class UserAggregate
{
    public Guid Id { get; init; }
    public required string Email { get; init; }
    public required string FullName { get; init; }
}
