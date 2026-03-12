namespace Account.Application.Inputs;

public sealed record CreateAdminInput(string Username, string Password, string[]? Permissions);
