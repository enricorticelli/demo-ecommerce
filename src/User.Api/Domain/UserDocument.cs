namespace User.Api.Domain;

public sealed class UserDocument
{
    public Guid Id { get; init; }
    public required string Email { get; init; }
    public required string FullName { get; init; }
}
