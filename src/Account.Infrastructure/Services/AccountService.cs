using Account.Application.Abstractions.Services;
using Account.Application.Inputs;
using Account.Application.Models;
using Account.Domain.Entities;
using Account.Infrastructure.Persistence;
using Account.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Shared.BuildingBlocks.Exceptions;

namespace Account.Infrastructure.Services;

public sealed class AccountService(AccountDbContext dbContext, TokenFactory tokenFactory, OrderApiClient orderApiClient) : IAccountService
{
    private static readonly string[] CustomerPermissions = ["account:read", "account:write", "orders:read"];
    private static readonly string[] AdminPermissions = ["catalog:read", "catalog:write", "orders:read", "shipping:read", "shipping:write"];

    public async Task<AuthTokenResult> RegisterCustomerAsync(RegisterCustomerInput request, CancellationToken cancellationToken)
    {
        var normalizedEmail = NormalizeEmail(request.Email);
        if (await dbContext.Users.AnyAsync(x => x.Realm == AccountRealm.Customer && x.NormalizedEmail == normalizedEmail, cancellationToken))
        {
            throw new ConflictAppException("An account with this email already exists.");
        }

        ValidatePassword(request.Password);
        var domainUser = AccountUser.CreateCustomer(
            request.Email,
            normalizedEmail,
            PasswordHasher.HashPassword(request.Password),
            request.FirstName,
            request.LastName,
            request.Phone);

        var entity = ToEntity(domainUser);
        dbContext.Users.Add(entity);

        await CreateEmailVerificationCodeAsync(entity.Id, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await IssueTokensAsync(entity, AccountRealm.Customer, cancellationToken);
    }

    public async Task<AuthTokenResult> LoginAsync(string realm, LoginInput request, CancellationToken cancellationToken)
    {
        var normalized = NormalizeEmail(request.Username);
        var userEntity = await dbContext.Users
            .FirstOrDefaultAsync(x => x.Realm == realm && (x.NormalizedEmail == normalized || x.Username == normalized), cancellationToken)
            ?? throw new ValidationAppException("Invalid credentials.");

        var domainUser = ToDomain(userEntity);
        if (!PasswordHasher.VerifyPassword(request.Password, domainUser.PasswordHash))
        {
            throw new ValidationAppException("Invalid credentials.");
        }

        if (realm == AccountRealm.Customer && domainUser.IsEmailVerified)
        {
            await orderApiClient.ClaimGuestOrdersAsync(domainUser.Id, domainUser.Email, cancellationToken);
        }

        return await IssueTokensAsync(userEntity, realm, cancellationToken);
    }

    public async Task<AuthTokenResult> RefreshAsync(string realm, string refreshToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new ValidationAppException("Refresh token is required.");
        }

        var refreshHash = PasswordHasher.HashToken(refreshToken.Trim());
        var session = await dbContext.RefreshSessions
            .FirstOrDefaultAsync(x => x.Realm == realm && x.TokenHash == refreshHash, cancellationToken)
            ?? throw new ValidationAppException("Invalid refresh token.");

        if (session.RevokedAtUtc.HasValue || session.ExpiresAtUtc <= DateTimeOffset.UtcNow)
        {
            throw new ValidationAppException("Refresh token expired or revoked.");
        }

        session.RevokedAtUtc = DateTimeOffset.UtcNow;

        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == session.UserId && x.Realm == realm, cancellationToken)
            ?? throw new NotFoundAppException("Account not found.");

        var tokens = await IssueTokensAsync(user, realm, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return tokens;
    }

    public async Task LogoutAsync(string realm, string refreshToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return;
        }

        var refreshHash = PasswordHasher.HashToken(refreshToken.Trim());
        var session = await dbContext.RefreshSessions
            .FirstOrDefaultAsync(x => x.Realm == realm && x.TokenHash == refreshHash, cancellationToken);

        if (session is null)
        {
            return;
        }

        session.RevokedAtUtc = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<(bool Issued, string? PreviewCode)> CreateEmailVerificationCodeByEmailAsync(string email, CancellationToken cancellationToken)
    {
        var normalized = NormalizeEmail(email);
        var user = await dbContext.Users.FirstOrDefaultAsync(
            x => x.Realm == AccountRealm.Customer && x.NormalizedEmail == normalized,
            cancellationToken);

        if (user is null)
        {
            return (false, null);
        }

        var code = await CreateEmailVerificationCodeAsync(user.Id, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return (true, code);
    }

    public async Task VerifyEmailAsync(VerifyEmailInput request, CancellationToken cancellationToken)
    {
        var normalized = NormalizeEmail(request.Email);
        var userEntity = await dbContext.Users.FirstOrDefaultAsync(
            x => x.Realm == AccountRealm.Customer && x.NormalizedEmail == normalized,
            cancellationToken)
            ?? throw new ValidationAppException("Invalid verification request.");

        var token = await dbContext.EmailVerificationTokens
            .Where(x => x.UserId == userEntity.Id && x.UsedAtUtc == null)
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new ValidationAppException("No active verification code.");

        if (token.ExpiresAtUtc <= DateTimeOffset.UtcNow)
        {
            throw new ValidationAppException("Verification code expired.");
        }

        if (!string.Equals(token.CodeHash, PasswordHasher.HashToken(request.Code.Trim()), StringComparison.Ordinal))
        {
            throw new ValidationAppException("Invalid verification code.");
        }

        token.UsedAtUtc = DateTimeOffset.UtcNow;

        var domainUser = ToDomain(userEntity);
        domainUser.VerifyEmail();
        ApplyDomain(domainUser, userEntity);

        await orderApiClient.ClaimGuestOrdersAsync(domainUser.Id, domainUser.Email, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<(bool Issued, string? PreviewCode)> CreatePasswordResetCodeAsync(ForgotPasswordInput request, CancellationToken cancellationToken)
    {
        var normalized = NormalizeEmail(request.Email);
        var user = await dbContext.Users.FirstOrDefaultAsync(
            x => x.Realm == AccountRealm.Customer && x.NormalizedEmail == normalized,
            cancellationToken);

        if (user is null)
        {
            return (false, null);
        }

        var code = GenerateNumericCode();
        dbContext.PasswordResetTokens.Add(new PasswordResetTokenEntity
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            CodeHash = PasswordHasher.HashToken(code),
            CreatedAtUtc = DateTimeOffset.UtcNow,
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(20)
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        return (true, code);
    }

    public async Task ResetPasswordAsync(ResetPasswordInput request, CancellationToken cancellationToken)
    {
        ValidatePassword(request.NewPassword);
        var normalized = NormalizeEmail(request.Email);
        var userEntity = await dbContext.Users.FirstOrDefaultAsync(
            x => x.Realm == AccountRealm.Customer && x.NormalizedEmail == normalized,
            cancellationToken)
            ?? throw new ValidationAppException("Invalid password reset request.");

        var token = await dbContext.PasswordResetTokens
            .Where(x => x.UserId == userEntity.Id && x.UsedAtUtc == null)
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new ValidationAppException("No active reset code.");

        if (token.ExpiresAtUtc <= DateTimeOffset.UtcNow)
        {
            throw new ValidationAppException("Reset code expired.");
        }

        if (!string.Equals(token.CodeHash, PasswordHasher.HashToken(request.Code.Trim()), StringComparison.Ordinal))
        {
            throw new ValidationAppException("Invalid reset code.");
        }

        token.UsedAtUtc = DateTimeOffset.UtcNow;

        var domainUser = ToDomain(userEntity);
        domainUser.ChangePasswordHash(PasswordHasher.HashPassword(request.NewPassword));
        ApplyDomain(domainUser, userEntity);

        var activeSessions = await dbContext.RefreshSessions.Where(x => x.UserId == userEntity.Id && x.RevokedAtUtc == null).ToListAsync(cancellationToken);
        foreach (var session in activeSessions)
        {
            session.RevokedAtUtc = DateTimeOffset.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<AccountUserModel> GetProfileAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == userId && x.Realm == AccountRealm.Customer, cancellationToken)
            ?? throw new NotFoundAppException("Customer profile not found.");

        return ToUserModel(ToDomain(user));
    }

    public async Task<AccountUserModel> UpdateProfileAsync(Guid userId, UpdateProfileInput request, CancellationToken cancellationToken)
    {
        var userEntity = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == userId && x.Realm == AccountRealm.Customer, cancellationToken)
            ?? throw new NotFoundAppException("Customer profile not found.");

        var domainUser = ToDomain(userEntity);
        domainUser.UpdateProfile(request.FirstName, request.LastName, request.Phone);
        ApplyDomain(domainUser, userEntity);

        await dbContext.SaveChangesAsync(cancellationToken);
        return ToUserModel(domainUser);
    }

    public async Task<IReadOnlyList<AccountAddressModel>> ListAddressesAsync(Guid userId, CancellationToken cancellationToken)
    {
        await EnsureCustomerAsync(userId, cancellationToken);
        var addresses = await dbContext.Addresses.Where(x => x.UserId == userId).OrderByDescending(x => x.CreatedAtUtc).ToArrayAsync(cancellationToken);
        return addresses.Select(ToDomain).Select(ToAddressModel).ToArray();
    }

    public async Task<AccountAddressModel> CreateAddressAsync(Guid userId, UpsertAddressInput request, CancellationToken cancellationToken)
    {
        await EnsureCustomerAsync(userId, cancellationToken);

        var domainAddress = AccountAddress.Create(
            userId,
            request.Label,
            request.Street,
            request.City,
            request.PostalCode,
            request.Country,
            request.IsDefaultShipping,
            request.IsDefaultBilling);

        var addressEntity = ToEntity(domainAddress);
        dbContext.Addresses.Add(addressEntity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToAddressModel(domainAddress);
    }

    public async Task<AccountAddressModel> UpdateAddressAsync(Guid userId, Guid addressId, UpsertAddressInput request, CancellationToken cancellationToken)
    {
        await EnsureCustomerAsync(userId, cancellationToken);
        var addressEntity = await dbContext.Addresses.FirstOrDefaultAsync(x => x.Id == addressId && x.UserId == userId, cancellationToken)
            ?? throw new NotFoundAppException("Address not found.");

        var domainAddress = ToDomain(addressEntity);
        domainAddress.Update(
            request.Label,
            request.Street,
            request.City,
            request.PostalCode,
            request.Country,
            request.IsDefaultShipping,
            request.IsDefaultBilling);

        ApplyDomain(domainAddress, addressEntity);

        await dbContext.SaveChangesAsync(cancellationToken);
        return ToAddressModel(domainAddress);
    }

    public async Task DeleteAddressAsync(Guid userId, Guid addressId, CancellationToken cancellationToken)
    {
        await EnsureCustomerAsync(userId, cancellationToken);
        var address = await dbContext.Addresses.FirstOrDefaultAsync(x => x.Id == addressId && x.UserId == userId, cancellationToken);
        if (address is null)
        {
            return;
        }

        dbContext.Remove(address);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<IReadOnlyList<OrderSummary>> ListMyOrdersAsync(Guid userId, CancellationToken cancellationToken)
    {
        return orderApiClient.ListByAuthenticatedUserAsync(userId, cancellationToken);
    }

    public async Task<AccountUserModel> GetAdminAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == userId && x.Realm == AccountRealm.Admin, cancellationToken)
            ?? throw new NotFoundAppException("Admin account not found.");

        return ToUserModel(ToDomain(user));
    }

    public async Task<IReadOnlyList<AccountAdminUserModel>> ListAdminsAsync(int limit, int offset, string? searchTerm, CancellationToken cancellationToken)
    {
        var query = dbContext.Users
            .Where(x => x.Realm == AccountRealm.Admin)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var token = searchTerm.Trim().ToLowerInvariant();
            query = query.Where(x =>
                x.Username.ToLower().Contains(token) ||
                x.Email.ToLower().Contains(token));
        }

        var admins = await query
            .OrderBy(x => x.Username)
            .Skip(offset)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return admins.Select(ToAdminUserModel).ToArray();
    }

    public async Task<AccountAdminUserModel> CreateAdminByAdminAsync(CreateAdminInput request, CancellationToken cancellationToken)
    {
        var normalized = NormalizeEmail(request.Username);
        var exists = await dbContext.Users.AnyAsync(x => x.Realm == AccountRealm.Admin && x.Username == normalized, cancellationToken);
        if (exists)
        {
            throw new ConflictAppException("Admin username already exists.");
        }

        ValidatePassword(request.Password);

        var domainAdmin = AccountUser.CreateAdmin(normalized, PasswordHasher.HashPassword(request.Password));
        dbContext.Users.Add(ToEntity(domainAdmin));
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToAdminUserModel(ToEntity(domainAdmin));
    }

    public async Task SetAdminPasswordByAdminAsync(Guid adminUserId, string newPassword, CancellationToken cancellationToken)
    {
        ValidatePassword(newPassword);

        var userEntity = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == adminUserId && x.Realm == AccountRealm.Admin, cancellationToken)
            ?? throw new NotFoundAppException("Admin account not found.");

        var domainUser = ToDomain(userEntity);
        domainUser.ChangePasswordHash(PasswordHasher.HashPassword(newPassword));
        ApplyDomain(domainUser, userEntity);

        var activeSessions = await dbContext.RefreshSessions
            .Where(x => x.UserId == adminUserId && x.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);

        foreach (var session in activeSessions)
        {
            session.RevokedAtUtc = DateTimeOffset.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAdminByAdminAsync(Guid actingAdminUserId, Guid adminUserId, CancellationToken cancellationToken)
    {
        if (actingAdminUserId == adminUserId)
        {
            throw new ValidationAppException("You cannot delete your own admin account.");
        }

        var adminToDelete = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == adminUserId && x.Realm == AccountRealm.Admin, cancellationToken)
            ?? throw new NotFoundAppException("Admin account not found.");

        var adminCount = await dbContext.Users.CountAsync(x => x.Realm == AccountRealm.Admin, cancellationToken);
        if (adminCount <= 1)
        {
            throw new ValidationAppException("Cannot delete the last admin account.");
        }

        var sessions = await dbContext.RefreshSessions
            .Where(x => x.UserId == adminUserId)
            .ToListAsync(cancellationToken);

        dbContext.RefreshSessions.RemoveRange(sessions);
        dbContext.Users.Remove(adminToDelete);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AccountCustomerAdminModel>> ListCustomersAsync(int limit, int offset, string? searchTerm, CancellationToken cancellationToken)
    {
        var query = dbContext.Users
            .Where(x => x.Realm == AccountRealm.Customer)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var token = searchTerm.Trim().ToLowerInvariant();
            query = query.Where(x =>
                x.Email.ToLower().Contains(token) ||
                x.Username.ToLower().Contains(token) ||
                x.FirstName.ToLower().Contains(token) ||
                x.LastName.ToLower().Contains(token) ||
                x.Phone.ToLower().Contains(token));
        }

        var customers = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip(offset)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return customers.Select(ToAdminCustomerModel).ToArray();
    }

    public async Task<AccountCustomerAdminModel> GetCustomerAsync(Guid customerId, CancellationToken cancellationToken)
    {
        var customer = await EnsureCustomerEntityAsync(customerId, cancellationToken);
        return ToAdminCustomerModel(customer);
    }

    public async Task<AccountCustomerAdminModel> UpdateCustomerByAdminAsync(Guid customerId, UpdateProfileInput request, CancellationToken cancellationToken)
    {
        var userEntity = await EnsureCustomerEntityAsync(customerId, cancellationToken);
        var domainUser = ToDomain(userEntity);
        domainUser.UpdateProfile(request.FirstName, request.LastName, request.Phone);
        ApplyDomain(domainUser, userEntity);

        await dbContext.SaveChangesAsync(cancellationToken);
        return ToAdminCustomerModel(userEntity);
    }

    public async Task SetCustomerPasswordByAdminAsync(Guid customerId, string newPassword, CancellationToken cancellationToken)
    {
        ValidatePassword(newPassword);

        var userEntity = await EnsureCustomerEntityAsync(customerId, cancellationToken);
        var domainUser = ToDomain(userEntity);
        domainUser.ChangePasswordHash(PasswordHasher.HashPassword(newPassword));
        ApplyDomain(domainUser, userEntity);

        var activeSessions = await dbContext.RefreshSessions
            .Where(x => x.UserId == customerId && x.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);

        foreach (var session in activeSessions)
        {
            session.RevokedAtUtc = DateTimeOffset.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AccountAddressModel>> ListCustomerAddressesByAdminAsync(Guid customerId, CancellationToken cancellationToken)
    {
        await EnsureCustomerAsync(customerId, cancellationToken);
        var addresses = await dbContext.Addresses
            .Where(x => x.UserId == customerId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToArrayAsync(cancellationToken);

        return addresses.Select(ToDomain).Select(ToAddressModel).ToArray();
    }

    public async Task<AccountAddressModel> CreateCustomerAddressByAdminAsync(Guid customerId, UpsertAddressInput request, CancellationToken cancellationToken)
    {
        await EnsureCustomerAsync(customerId, cancellationToken);

        var domainAddress = AccountAddress.Create(
            customerId,
            request.Label,
            request.Street,
            request.City,
            request.PostalCode,
            request.Country,
            request.IsDefaultShipping,
            request.IsDefaultBilling);

        dbContext.Addresses.Add(ToEntity(domainAddress));
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToAddressModel(domainAddress);
    }

    public async Task<AccountAddressModel> UpdateCustomerAddressByAdminAsync(Guid customerId, Guid addressId, UpsertAddressInput request, CancellationToken cancellationToken)
    {
        await EnsureCustomerAsync(customerId, cancellationToken);

        var addressEntity = await dbContext.Addresses.FirstOrDefaultAsync(
            x => x.Id == addressId && x.UserId == customerId,
            cancellationToken)
            ?? throw new NotFoundAppException("Address not found.");

        var domainAddress = ToDomain(addressEntity);
        domainAddress.Update(
            request.Label,
            request.Street,
            request.City,
            request.PostalCode,
            request.Country,
            request.IsDefaultShipping,
            request.IsDefaultBilling);

        ApplyDomain(domainAddress, addressEntity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToAddressModel(domainAddress);
    }

    public async Task DeleteCustomerAddressByAdminAsync(Guid customerId, Guid addressId, CancellationToken cancellationToken)
    {
        await EnsureCustomerAsync(customerId, cancellationToken);

        var address = await dbContext.Addresses.FirstOrDefaultAsync(
            x => x.Id == addressId && x.UserId == customerId,
            cancellationToken);

        if (address is null)
        {
            return;
        }

        dbContext.Remove(address);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public static string[] GetPermissionsForRealm(string realm)
    {
        return realm == AccountRealm.Admin ? AdminPermissions : CustomerPermissions;
    }

    public async Task EnsureDefaultAdminAsync(string username, string password, CancellationToken cancellationToken)
    {
        var normalized = NormalizeEmail(username);
        var exists = await dbContext.Users.AnyAsync(x => x.Realm == AccountRealm.Admin && x.Username == normalized, cancellationToken);
        if (exists)
        {
            return;
        }

        ValidatePassword(password);

        var domainAdmin = AccountUser.CreateAdmin(normalized, PasswordHasher.HashPassword(password));
        dbContext.Users.Add(ToEntity(domainAdmin));

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<AuthTokenResult> IssueTokensAsync(AccountUserEntity user, string realm, CancellationToken cancellationToken)
    {
        var permissions = GetPermissionsForRealm(realm);
        var (accessToken, accessExpiresAt) = tokenFactory.CreateAccessToken(user.Id, user.Email, user.IsEmailVerified, realm, permissions);
        var (refreshToken, refreshExpiresAt) = tokenFactory.CreateRefreshToken();

        dbContext.RefreshSessions.Add(new RefreshSessionEntity
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Realm = realm,
            TokenHash = PasswordHasher.HashToken(refreshToken),
            CreatedAtUtc = DateTimeOffset.UtcNow,
            ExpiresAtUtc = refreshExpiresAt
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        return new AuthTokenResult(
            accessToken,
            accessExpiresAt,
            refreshToken,
            refreshExpiresAt,
            realm,
            user.Id,
            user.Email,
            permissions);
    }

    private async Task<string> CreateEmailVerificationCodeAsync(Guid userId, CancellationToken cancellationToken)
    {
        var code = GenerateNumericCode();
        dbContext.EmailVerificationTokens.Add(new EmailVerificationTokenEntity
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CodeHash = PasswordHasher.HashToken(code),
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(20),
            CreatedAtUtc = DateTimeOffset.UtcNow
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        return code;
    }

    private async Task EnsureCustomerAsync(Guid userId, CancellationToken cancellationToken)
    {
        var exists = await dbContext.Users.AnyAsync(x => x.Id == userId && x.Realm == AccountRealm.Customer, cancellationToken);
        if (!exists)
        {
            throw new NotFoundAppException("Customer profile not found.");
        }
    }

    private async Task<AccountUserEntity> EnsureCustomerEntityAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await dbContext.Users.FirstOrDefaultAsync(x => x.Id == userId && x.Realm == AccountRealm.Customer, cancellationToken)
            ?? throw new NotFoundAppException("Customer profile not found.");
    }

    private static string NormalizeEmail(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ValidationAppException("Email/username is required.");
        }

        return value.Trim().ToLowerInvariant();
    }

    private static void ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Trim().Length < 8)
        {
            throw new ValidationAppException("Password must be at least 8 characters long.");
        }
    }

    private static string GenerateNumericCode()
    {
        var number = Random.Shared.Next(100000, 999999);
        return number.ToString();
    }

    private static AccountUser ToDomain(AccountUserEntity entity)
    {
        return AccountUser.Restore(
            entity.Id,
            entity.Realm,
            entity.Username,
            entity.Email,
            entity.NormalizedEmail,
            entity.PasswordHash,
            entity.IsEmailVerified,
            entity.FirstName,
            entity.LastName,
            entity.Phone,
            entity.CreatedAtUtc);
    }

    private static AccountAddress ToDomain(CustomerAddressEntity entity)
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

    private static AccountUserEntity ToEntity(AccountUser domain)
    {
        return new AccountUserEntity
        {
            Id = domain.Id,
            Realm = domain.Realm,
            Username = domain.Username,
            Email = domain.Email,
            NormalizedEmail = domain.NormalizedEmail,
            PasswordHash = domain.PasswordHash,
            IsEmailVerified = domain.IsEmailVerified,
            FirstName = domain.FirstName,
            LastName = domain.LastName,
            Phone = domain.Phone,
            CreatedAtUtc = domain.CreatedAtUtc
        };
    }

    private static CustomerAddressEntity ToEntity(AccountAddress domain)
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

    private static void ApplyDomain(AccountUser domain, AccountUserEntity entity)
    {
        entity.PasswordHash = domain.PasswordHash;
        entity.IsEmailVerified = domain.IsEmailVerified;
        entity.FirstName = domain.FirstName;
        entity.LastName = domain.LastName;
        entity.Phone = domain.Phone;
    }

    private static void ApplyDomain(AccountAddress domain, CustomerAddressEntity entity)
    {
        entity.Label = domain.Label;
        entity.Street = domain.Street;
        entity.City = domain.City;
        entity.PostalCode = domain.PostalCode;
        entity.Country = domain.Country;
        entity.IsDefaultShipping = domain.IsDefaultShipping;
        entity.IsDefaultBilling = domain.IsDefaultBilling;
    }

    private static AccountUserModel ToUserModel(AccountUser user)
    {
        return new AccountUserModel(
            user.Id,
            user.Email,
            user.IsEmailVerified,
            user.FirstName,
            user.LastName,
            user.Phone);
    }

    private static AccountAddressModel ToAddressModel(AccountAddress address)
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

    private static AccountCustomerAdminModel ToAdminCustomerModel(AccountUserEntity user)
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

    private static AccountAdminUserModel ToAdminUserModel(AccountUserEntity user)
    {
        return new AccountAdminUserModel(
            user.Id,
            user.Username,
            user.Email,
            user.CreatedAtUtc);
    }
}
