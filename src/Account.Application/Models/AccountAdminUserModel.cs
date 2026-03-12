namespace Account.Application.Models;

public sealed record AccountAdminUserModel(
    Guid Id,
    string Username,
    string Email,
    DateTimeOffset CreatedAtUtc,
    string[] Permissions,
    bool HasCustomPermissions);
