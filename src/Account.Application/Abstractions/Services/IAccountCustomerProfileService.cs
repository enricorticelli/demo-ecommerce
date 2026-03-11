using Account.Application.Inputs;
using Account.Application.Models;

namespace Account.Application.Abstractions.Services;

public interface IAccountCustomerProfileService
{
    Task<AccountUserModel> GetProfileAsync(Guid userId, CancellationToken cancellationToken);
    Task<AccountUserModel> UpdateProfileAsync(Guid userId, UpdateProfileInput request, CancellationToken cancellationToken);
    Task<IReadOnlyList<AccountAddressModel>> ListAddressesAsync(Guid userId, CancellationToken cancellationToken);
    Task<AccountAddressModel> CreateAddressAsync(Guid userId, UpsertAddressInput request, CancellationToken cancellationToken);
    Task<AccountAddressModel> UpdateAddressAsync(Guid userId, Guid addressId, UpsertAddressInput request, CancellationToken cancellationToken);
    Task DeleteAddressAsync(Guid userId, Guid addressId, CancellationToken cancellationToken);
    Task<IReadOnlyList<OrderSummary>> ListMyOrdersAsync(Guid userId, CancellationToken cancellationToken);
}
