namespace Order.Api.Contracts.Requests;

public sealed record OrderAddressRequest(string Street, string City, string PostalCode, string Country);
