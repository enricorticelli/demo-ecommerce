namespace Account.Application.Models;

public sealed record AccountCustomerAdminModel(
    Guid Id,
    string Username,
    string Email,
    bool IsEmailVerified,
    string? FirstName,
    string? LastName,
    string? Phone,
    DateTimeOffset CreatedAtUtc);
