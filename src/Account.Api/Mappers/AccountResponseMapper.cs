using Account.Api.Contracts.Responses;
using Account.Application.Models;

namespace Account.Api.Mappers;

public static class AccountResponseMapper
{
    public static AuthResponse ToResponse(this AuthTokenResult auth)
    {
        return new AuthResponse(
            auth.AccessToken,
            auth.AccessTokenExpiresAtUtc.ToString("O"),
            auth.RefreshToken,
            auth.RefreshTokenExpiresAtUtc.ToString("O"),
            auth.Realm,
            auth.UserId,
            auth.Email,
            auth.Permissions);
    }

    public static ProfileResponse ToResponse(this AccountUserModel user)
    {
        return new ProfileResponse(
            user.Id,
            user.Email,
            user.IsEmailVerified,
            user.FirstName ?? string.Empty,
            user.LastName ?? string.Empty,
            user.Phone ?? string.Empty);
    }

    public static AddressResponse ToResponse(this AccountAddressModel address)
    {
        return new AddressResponse(
            address.Id,
            address.Label,
            address.Street,
            address.City,
            address.PostalCode,
            address.Country,
            address.IsDefaultShipping,
            address.IsDefaultBilling);
    }

    public static AdminCustomerResponse ToResponse(this AccountCustomerAdminModel customer)
    {
        return new AdminCustomerResponse(
            customer.Id,
            customer.Username,
            customer.Email,
            customer.IsEmailVerified,
            customer.FirstName ?? string.Empty,
            customer.LastName ?? string.Empty,
            customer.Phone ?? string.Empty,
            customer.CreatedAtUtc.ToString("O"));
    }

    public static AdminAccountUserResponse ToResponse(this AccountAdminUserModel admin)
    {
        return new AdminAccountUserResponse(
            admin.Id,
            admin.Username,
            admin.Email,
            admin.CreatedAtUtc.ToString("O"));
    }

    public static OrderSummaryResponse ToResponse(this OrderSummary order)
    {
        return new OrderSummaryResponse(
            order.Id,
            order.Status,
            order.TotalAmount,
            order.CreatedAtUtc.ToString("O"),
            order.TrackingCode,
            order.TransactionId,
            order.FailureReason);
    }

    public static PermissionsResponse ToPermissionsResponse(this string realm, string[] permissions)
    {
        var role = realm == "admin" ? "admin" : "customer";
        return new PermissionsResponse(role, permissions);
    }
}
