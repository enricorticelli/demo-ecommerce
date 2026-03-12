namespace Account.Api.Contracts.Requests;

public sealed record AdminCreateAdminUserRequest(string Username, string Password, string[]? Permissions, bool IsSuperUser = false);
