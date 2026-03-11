using Account.Application.Models;
using Account.Domain.Entities;
using Account.Infrastructure.Persistence.Entities;

namespace Account.Infrastructure.Mappers;

internal static class AccountModelMapper
{
    public static AccountUserModel ToUserModel(AccountUser user)
    {
        return new AccountUserModel(
            user.Id,
            user.Email,
            user.IsEmailVerified,
            user.FirstName,
            user.LastName,
            user.Phone);
    }

    public static AccountAddressModel ToAddressModel(AccountAddress address)
    {
        return new AccountAddressModel(
            address.Id,
            address.UserId,
            address.Label,
            address.Street,
            address.City,
            address.PostalCode,
            address.Country,
            address.IsDefaultShipping,
            address.IsDefaultBilling);
    }

    public static AccountCustomerAdminModel ToAdminCustomerModel(AccountUserEntity user)
    {
        return new AccountCustomerAdminModel(
            user.Id,
            user.Username,
            user.Email,
            user.IsEmailVerified,
            user.FirstName,
            user.LastName,
            user.Phone,
            user.CreatedAtUtc);
    }

    public static AccountAdminUserModel ToAdminUserModel(AccountUserEntity user)
    {
        return new AccountAdminUserModel(
            user.Id,
            user.Username,
            user.Email,
            user.CreatedAtUtc);
    }
}
