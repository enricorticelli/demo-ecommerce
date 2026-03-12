using Account.Application.Abstractions.Services;
using Account.Application.Inputs;
using Account.Application.Models;
using Account.Domain.Entities;
using Account.Infrastructure.Mappers;
using Account.Infrastructure.Persistence;
using Account.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Shared.BuildingBlocks.Api;
using Shared.BuildingBlocks.Exceptions;
using Shared.BuildingBlocks.Helpers;

namespace Account.Infrastructure.Services;

public sealed class AccountAdministrationService(AccountDbContext dbContext) : IAccountAdministrationService
{
    private static readonly HashSet<string> AllowedAdminPermissions = new(AuthorizationPermissions.AllAdmin, StringComparer.Ordinal);

    public async Task<AccountUserModel> GetAdminAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == userId && x.Realm == AccountRealm.Admin, cancellationToken)
            ?? throw new NotFoundAppException("Admin account not found.");

        return AccountModelMapper.ToUserModel(AccountUserEntityMapper.ToDomain(user));
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

        return admins.Select(x => AccountModelMapper.ToAdminUserModel(x, ResolveAdminPermissions(x))).ToArray();
    }

    public async Task<AccountAdminUserModel> CreateAdminByAdminAsync(CreateAdminInput request, CancellationToken cancellationToken)
    {
        var normalized = InputValidationHelper.NormalizeRequiredLowerInvariant(request.Username, "Email/username");
        var exists = await dbContext.Users.AnyAsync(x => x.Realm == AccountRealm.Admin && x.Username == normalized, cancellationToken);
        if (exists)
        {
            throw new ConflictAppException("Admin username already exists.");
        }

        InputValidationHelper.EnsureMinLength(request.Password, 8, "Password must be at least 8 characters long.");

        var domainAdmin = AccountUser.CreateAdmin(normalized, PasswordHasher.HashPassword(request.Password));
        var entity = AccountUserEntityMapper.ToEntity(domainAdmin);
        entity.CustomPermissions = NormalizeAdminPermissionsForCreate(request.Permissions);

        dbContext.Users.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return AccountModelMapper.ToAdminUserModel(entity, ResolveAdminPermissions(entity));
    }

    public async Task SetAdminPermissionsByAdminAsync(Guid actingAdminUserId, Guid adminUserId, string[]? permissions, CancellationToken cancellationToken)
    {
        var adminToUpdate = await dbContext.Users
            .FirstOrDefaultAsync(x => x.Id == adminUserId && x.Realm == AccountRealm.Admin, cancellationToken)
            ?? throw new NotFoundAppException("Admin account not found.");

        var normalizedPermissions = NormalizeAdminPermissionsForUpdate(permissions);
        var effectivePermissions = normalizedPermissions ?? AuthorizationPermissions.AllAdmin;

        if (actingAdminUserId == adminUserId && !effectivePermissions.Contains(AuthorizationPermissions.AccountWrite, StringComparer.Ordinal))
        {
            throw new ValidationAppException("You cannot remove your own account:write permission.");
        }

        adminToUpdate.CustomPermissions = normalizedPermissions;

        var activeSessions = await dbContext.RefreshSessions
            .Where(x => x.UserId == adminUserId && x.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);

        foreach (var session in activeSessions)
        {
            session.RevokedAtUtc = DateTimeOffset.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SetAdminPasswordByAdminAsync(Guid adminUserId, string newPassword, CancellationToken cancellationToken)
    {
        InputValidationHelper.EnsureMinLength(newPassword, 8, "Password must be at least 8 characters long.");

        var userEntity = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == adminUserId && x.Realm == AccountRealm.Admin, cancellationToken)
            ?? throw new NotFoundAppException("Admin account not found.");

        var domainUser = AccountUserEntityMapper.ToDomain(userEntity);
        domainUser.ChangePasswordHash(PasswordHasher.HashPassword(newPassword));
        AccountUserEntityMapper.ApplyDomain(domainUser, userEntity);

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

        return customers.Select(AccountModelMapper.ToAdminCustomerModel).ToArray();
    }

    public async Task<AccountCustomerAdminModel> GetCustomerAsync(Guid customerId, CancellationToken cancellationToken)
    {
        var customer = await EnsureCustomerEntityAsync(customerId, cancellationToken);
        return AccountModelMapper.ToAdminCustomerModel(customer);
    }

    public async Task<AccountCustomerAdminModel> UpdateCustomerByAdminAsync(Guid customerId, UpdateProfileInput request, CancellationToken cancellationToken)
    {
        var userEntity = await EnsureCustomerEntityAsync(customerId, cancellationToken);
        var domainUser = AccountUserEntityMapper.ToDomain(userEntity);
        domainUser.UpdateProfile(request.FirstName, request.LastName, request.Phone);
        AccountUserEntityMapper.ApplyDomain(domainUser, userEntity);

        await dbContext.SaveChangesAsync(cancellationToken);
        return AccountModelMapper.ToAdminCustomerModel(userEntity);
    }

    public async Task SetCustomerPasswordByAdminAsync(Guid customerId, string newPassword, CancellationToken cancellationToken)
    {
        InputValidationHelper.EnsureMinLength(newPassword, 8, "Password must be at least 8 characters long.");

        var userEntity = await EnsureCustomerEntityAsync(customerId, cancellationToken);
        var domainUser = AccountUserEntityMapper.ToDomain(userEntity);
        domainUser.ChangePasswordHash(PasswordHasher.HashPassword(newPassword));
        AccountUserEntityMapper.ApplyDomain(domainUser, userEntity);

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

        return addresses.Select(AccountAddressEntityMapper.ToDomain).Select(AccountModelMapper.ToAddressModel).ToArray();
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

        dbContext.Addresses.Add(AccountAddressEntityMapper.ToEntity(domainAddress));
        await dbContext.SaveChangesAsync(cancellationToken);
        return AccountModelMapper.ToAddressModel(domainAddress);
    }

    public async Task<AccountAddressModel> UpdateCustomerAddressByAdminAsync(Guid customerId, Guid addressId, UpsertAddressInput request, CancellationToken cancellationToken)
    {
        await EnsureCustomerAsync(customerId, cancellationToken);

        var addressEntity = await dbContext.Addresses.FirstOrDefaultAsync(
            x => x.Id == addressId && x.UserId == customerId,
            cancellationToken)
            ?? throw new NotFoundAppException("Address not found.");

        var domainAddress = AccountAddressEntityMapper.ToDomain(addressEntity);
        domainAddress.Update(
            request.Label,
            request.Street,
            request.City,
            request.PostalCode,
            request.Country,
            request.IsDefaultShipping,
            request.IsDefaultBilling);

        AccountAddressEntityMapper.ApplyDomain(domainAddress, addressEntity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return AccountModelMapper.ToAddressModel(domainAddress);
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

    private static string[] ResolveAdminPermissions(AccountUserEntity admin)
    {
        return admin.CustomPermissions is not null
            ? admin.CustomPermissions
            : AuthorizationPermissions.AllAdmin;
    }

    private static string[]? NormalizeAdminPermissionsForCreate(IEnumerable<string>? permissions)
    {
        if (permissions is null)
        {
            return null;
        }

        return NormalizeAdminPermissionsForUpdate(permissions);
    }

    private static string[]? NormalizeAdminPermissionsForUpdate(IEnumerable<string>? permissions)
    {
        if (permissions is null)
        {
            return null;
        }

        var requested = permissions.ToArray();
        if (requested.Length == 0)
        {
            return [];
        }

        var normalized = requested
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (normalized.Any(x => !AllowedAdminPermissions.Contains(x)))
        {
            throw new ValidationAppException("Invalid admin permissions set.");
        }

        return normalized;
    }
}
