using Account.Application.Abstractions.Services;
using Account.Application.Inputs;
using Account.Application.Models;
using Account.Domain.Entities;
using Account.Infrastructure.Mappers;
using Account.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.BuildingBlocks.Exceptions;

namespace Account.Infrastructure.Services;

public sealed class AccountCustomerProfileService(AccountDbContext dbContext, OrderApiClient orderApiClient) : IAccountCustomerProfileService
{
    public async Task<AccountUserModel> GetProfileAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == userId && x.Realm == AccountRealm.Customer, cancellationToken)
            ?? throw new NotFoundAppException("Customer profile not found.");

        return AccountModelMapper.ToUserModel(AccountUserEntityMapper.ToDomain(user));
    }

    public async Task<AccountUserModel> UpdateProfileAsync(Guid userId, UpdateProfileInput request, CancellationToken cancellationToken)
    {
        var userEntity = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == userId && x.Realm == AccountRealm.Customer, cancellationToken)
            ?? throw new NotFoundAppException("Customer profile not found.");

        var domainUser = AccountUserEntityMapper.ToDomain(userEntity);
        domainUser.UpdateProfile(request.FirstName, request.LastName, request.Phone);
        AccountUserEntityMapper.ApplyDomain(domainUser, userEntity);

        await dbContext.SaveChangesAsync(cancellationToken);
        return AccountModelMapper.ToUserModel(domainUser);
    }

    public async Task<IReadOnlyList<AccountAddressModel>> ListAddressesAsync(Guid userId, CancellationToken cancellationToken)
    {
        await EnsureCustomerAsync(userId, cancellationToken);
        var addresses = await dbContext.Addresses.Where(x => x.UserId == userId).OrderByDescending(x => x.CreatedAtUtc).ToArrayAsync(cancellationToken);
        return addresses.Select(AccountAddressEntityMapper.ToDomain).Select(AccountModelMapper.ToAddressModel).ToArray();
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

        var addressEntity = AccountAddressEntityMapper.ToEntity(domainAddress);
        dbContext.Addresses.Add(addressEntity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return AccountModelMapper.ToAddressModel(domainAddress);
    }

    public async Task<AccountAddressModel> UpdateAddressAsync(Guid userId, Guid addressId, UpsertAddressInput request, CancellationToken cancellationToken)
    {
        await EnsureCustomerAsync(userId, cancellationToken);
        var addressEntity = await dbContext.Addresses.FirstOrDefaultAsync(x => x.Id == addressId && x.UserId == userId, cancellationToken)
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

    public Task<IReadOnlyList<OrderSummary>> ListMyOrdersAsync(Guid userId, string accessToken, CancellationToken cancellationToken)
    {
        return orderApiClient.ListByAuthenticatedUserAsync(accessToken, cancellationToken);
    }

    private async Task EnsureCustomerAsync(Guid userId, CancellationToken cancellationToken)
    {
        var exists = await dbContext.Users.AnyAsync(x => x.Id == userId && x.Realm == AccountRealm.Customer, cancellationToken);
        if (!exists)
        {
            throw new NotFoundAppException("Customer profile not found.");
        }
    }
}
