namespace Account.Api.Contracts.Responses;

public sealed record AdminCustomerResponse(
    Guid Id,
    string Username,
    string Email,
    bool IsEmailVerified,
    string FirstName,
    string LastName,
    string Phone,
    string CreatedAtUtc);
