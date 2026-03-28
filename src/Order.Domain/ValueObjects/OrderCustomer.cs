using Shared.BuildingBlocks.Exceptions;

namespace Order.Domain.ValueObjects;

public sealed class OrderCustomer
{
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string Phone { get; private set; } = string.Empty;

    private OrderCustomer()
    {
    }

    private OrderCustomer(string firstName, string lastName, string email, string phone)
    {
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        Phone = phone;
    }

    public static OrderCustomer Create(string firstName, string lastName, string email, string? phone)
    {
        if (string.IsNullOrWhiteSpace(firstName))
        {
            throw new ValidationAppException("Customer first name is required.");
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            throw new ValidationAppException("Customer last name is required.");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ValidationAppException("Customer email is required.");
        }

        return new OrderCustomer(firstName.Trim(), lastName.Trim(), email.Trim(), phone?.Trim() ?? string.Empty);
    }
}
