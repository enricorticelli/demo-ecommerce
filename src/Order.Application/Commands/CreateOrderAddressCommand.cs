namespace Order.Application.Commands;

public sealed record CreateOrderAddressCommand(string Street, string City, string PostalCode, string Country);
