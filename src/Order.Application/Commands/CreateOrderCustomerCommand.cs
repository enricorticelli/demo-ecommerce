namespace Order.Application.Commands;

public sealed record CreateOrderCustomerCommand(string FirstName, string LastName, string Email, string Phone);
