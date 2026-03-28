using Shared.BuildingBlocks.Exceptions;

namespace Order.Domain.ValueObjects;

public sealed class OrderAddress
{
    public string Street { get; private set; } = string.Empty;
    public string City { get; private set; } = string.Empty;
    public string PostalCode { get; private set; } = string.Empty;
    public string Country { get; private set; } = string.Empty;

    private OrderAddress()
    {
    }

    private OrderAddress(string street, string city, string postalCode, string country)
    {
        Street = street;
        City = city;
        PostalCode = postalCode;
        Country = country;
    }

    public static OrderAddress Create(string street, string city, string postalCode, string country)
    {
        if (string.IsNullOrWhiteSpace(street))
        {
            throw new ValidationAppException("Address street is required.");
        }

        if (string.IsNullOrWhiteSpace(city))
        {
            throw new ValidationAppException("Address city is required.");
        }

        if (string.IsNullOrWhiteSpace(postalCode))
        {
            throw new ValidationAppException("Address postal code is required.");
        }

        if (string.IsNullOrWhiteSpace(country))
        {
            throw new ValidationAppException("Address country is required.");
        }

        return new OrderAddress(street.Trim(), city.Trim(), postalCode.Trim(), country.Trim());
    }
}
