namespace Order.Api.Contracts.Responses;

public sealed record OrderAddressResponse(string Street, string City, string PostalCode, string Country);
