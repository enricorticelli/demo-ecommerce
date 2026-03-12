namespace Account.Api.Contracts.Responses;

public sealed record AdminAccountUserResponse(
    Guid Id,
    string Username,
    string Email,
    string CreatedAtUtc,
    string[] Permissions,
    bool HasCustomPermissions,
    bool IsSuperUser);
