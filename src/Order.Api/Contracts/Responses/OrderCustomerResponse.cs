namespace Order.Api.Contracts.Responses;

public sealed record OrderCustomerResponse(string FirstName, string LastName, string Email, string Phone);
