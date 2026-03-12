using Account.Application.Inputs;
using Account.Application.Models;

namespace Account.Application.Abstractions.Services;

public interface IAccountAdministrationService
{
    Task<AccountUserModel> GetAdminAsync(Guid userId, CancellationToken cancellationToken);
    Task<IReadOnlyList<AccountAdminUserModel>> ListAdminsAsync(Guid actingAdminUserId, int limit, int offset, string? searchTerm, CancellationToken cancellationToken);
    Task<AccountAdminUserModel> CreateAdminByAdminAsync(Guid actingAdminUserId, CreateAdminInput request, CancellationToken cancellationToken);
    Task SetAdminPermissionsByAdminAsync(Guid actingAdminUserId, Guid adminUserId, string[]? permissions, CancellationToken cancellationToken);
    Task SetAdminPasswordByAdminAsync(Guid actingAdminUserId, Guid adminUserId, string newPassword, CancellationToken cancellationToken);
    Task DeleteAdminByAdminAsync(Guid actingAdminUserId, Guid adminUserId, CancellationToken cancellationToken);
    Task<IReadOnlyList<AccountCustomerAdminModel>> ListCustomersAsync(int limit, int offset, string? searchTerm, CancellationToken cancellationToken);
    Task<AccountCustomerAdminModel> GetCustomerAsync(Guid customerId, CancellationToken cancellationToken);
    Task<AccountCustomerAdminModel> UpdateCustomerByAdminAsync(Guid customerId, UpdateProfileInput request, CancellationToken cancellationToken);
    Task SetCustomerPasswordByAdminAsync(Guid customerId, string newPassword, CancellationToken cancellationToken);
    Task<IReadOnlyList<AccountAddressModel>> ListCustomerAddressesByAdminAsync(Guid customerId, CancellationToken cancellationToken);
    Task<AccountAddressModel> CreateCustomerAddressByAdminAsync(Guid customerId, UpsertAddressInput request, CancellationToken cancellationToken);
    Task<AccountAddressModel> UpdateCustomerAddressByAdminAsync(Guid customerId, Guid addressId, UpsertAddressInput request, CancellationToken cancellationToken);
    Task DeleteCustomerAddressByAdminAsync(Guid customerId, Guid addressId, CancellationToken cancellationToken);
}
