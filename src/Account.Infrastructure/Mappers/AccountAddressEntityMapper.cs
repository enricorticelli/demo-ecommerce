using Account.Domain.Entities;
using Account.Infrastructure.Persistence.Entities;

namespace Account.Infrastructure.Mappers;

internal static class AccountAddressEntityMapper
{
    public static AccountAddress ToDomain(CustomerAddressEntity entity)
    {
        return AccountAddress.Restore(
            entity.Id,
            entity.UserId,
            entity.Label,
            entity.Street,
            entity.City,
            entity.PostalCode,
            entity.Country,
            entity.IsDefaultShipping,
            entity.IsDefaultBilling,
            entity.CreatedAtUtc);
    }

    public static CustomerAddressEntity ToEntity(AccountAddress domain)
    {
        return new CustomerAddressEntity
        {
            Id = domain.Id,
            UserId = domain.UserId,
            Label = domain.Label,
            Street = domain.Street,
            City = domain.City,
            PostalCode = domain.PostalCode,
            Country = domain.Country,
            IsDefaultShipping = domain.IsDefaultShipping,
            IsDefaultBilling = domain.IsDefaultBilling,
            CreatedAtUtc = domain.CreatedAtUtc
        };
    }

    public static void ApplyDomain(AccountAddress domain, CustomerAddressEntity entity)
    {
        entity.Label = domain.Label;
        entity.Street = domain.Street;
        entity.City = domain.City;
        entity.PostalCode = domain.PostalCode;
        entity.Country = domain.Country;
        entity.IsDefaultShipping = domain.IsDefaultShipping;
        entity.IsDefaultBilling = domain.IsDefaultBilling;
    }
}
