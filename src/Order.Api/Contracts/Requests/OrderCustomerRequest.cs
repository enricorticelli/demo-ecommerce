namespace Order.Api.Contracts.Requests;

public sealed record OrderCustomerRequest(string FirstName, string LastName, string Email, string? Phone);
