using Account.Application.Inputs;
using Account.Application.Models;

namespace Account.Application.Abstractions.Services;

public interface IAccountService
{
    Task<AuthTokenResult> RegisterCustomerAsync(RegisterCustomerInput request, CancellationToken cancellationToken);
    Task<AuthTokenResult> LoginAsync(string realm, LoginInput request, CancellationToken cancellationToken);
    Task<AuthTokenResult> RefreshAsync(string realm, string refreshToken, CancellationToken cancellationToken);
    Task LogoutAsync(string realm, string refreshToken, CancellationToken cancellationToken);
    Task<(bool Issued, string? PreviewCode)> CreateEmailVerificationCodeByEmailAsync(string email, CancellationToken cancellationToken);
    Task VerifyEmailAsync(VerifyEmailInput request, CancellationToken cancellationToken);
    Task<(bool Issued, string? PreviewCode)> CreatePasswordResetCodeAsync(ForgotPasswordInput request, CancellationToken cancellationToken);
    Task ResetPasswordAsync(ResetPasswordInput request, CancellationToken cancellationToken);
    Task<AccountUserModel> GetProfileAsync(Guid userId, CancellationToken cancellationToken);
    Task<AccountUserModel> UpdateProfileAsync(Guid userId, UpdateProfileInput request, CancellationToken cancellationToken);
    Task<IReadOnlyList<AccountAddressModel>> ListAddressesAsync(Guid userId, CancellationToken cancellationToken);
    Task<AccountAddressModel> CreateAddressAsync(Guid userId, UpsertAddressInput request, CancellationToken cancellationToken);
    Task<AccountAddressModel> UpdateAddressAsync(Guid userId, Guid addressId, UpsertAddressInput request, CancellationToken cancellationToken);
    Task DeleteAddressAsync(Guid userId, Guid addressId, CancellationToken cancellationToken);
    Task<IReadOnlyList<OrderSummary>> ListMyOrdersAsync(Guid userId, CancellationToken cancellationToken);
    Task<AccountUserModel> GetAdminAsync(Guid userId, CancellationToken cancellationToken);
    Task<IReadOnlyList<AccountAdminUserModel>> ListAdminsAsync(int limit, int offset, string? searchTerm, CancellationToken cancellationToken);
    Task<AccountAdminUserModel> CreateAdminByAdminAsync(CreateAdminInput request, CancellationToken cancellationToken);
    Task SetAdminPasswordByAdminAsync(Guid adminUserId, string newPassword, CancellationToken cancellationToken);
    Task DeleteAdminByAdminAsync(Guid actingAdminUserId, Guid adminUserId, CancellationToken cancellationToken);
    Task<IReadOnlyList<AccountCustomerAdminModel>> ListCustomersAsync(int limit, int offset, string? searchTerm, CancellationToken cancellationToken);
    Task<AccountCustomerAdminModel> GetCustomerAsync(Guid customerId, CancellationToken cancellationToken);
    Task<AccountCustomerAdminModel> UpdateCustomerByAdminAsync(Guid customerId, UpdateProfileInput request, CancellationToken cancellationToken);
    Task SetCustomerPasswordByAdminAsync(Guid customerId, string newPassword, CancellationToken cancellationToken);
    Task<IReadOnlyList<AccountAddressModel>> ListCustomerAddressesByAdminAsync(Guid customerId, CancellationToken cancellationToken);
    Task<AccountAddressModel> CreateCustomerAddressByAdminAsync(Guid customerId, UpsertAddressInput request, CancellationToken cancellationToken);
    Task<AccountAddressModel> UpdateCustomerAddressByAdminAsync(Guid customerId, Guid addressId, UpsertAddressInput request, CancellationToken cancellationToken);
    Task DeleteCustomerAddressByAdminAsync(Guid customerId, Guid addressId, CancellationToken cancellationToken);
    Task EnsureDefaultAdminAsync(string username, string password, CancellationToken cancellationToken);
}
