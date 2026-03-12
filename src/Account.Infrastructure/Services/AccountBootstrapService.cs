using Account.Application.Abstractions.Services;
using Account.Application.Models;
using Account.Domain.Entities;
using Account.Infrastructure.Mappers;
using Account.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.BuildingBlocks.Helpers;

namespace Account.Infrastructure.Services;

public sealed class AccountBootstrapService(AccountDbContext dbContext) : IAccountBootstrapService
{
    public async Task EnsureDefaultAdminAsync(string username, string password, CancellationToken cancellationToken)
    {
        var normalized = InputValidationHelper.NormalizeRequiredLowerInvariant(username, "Email/username");
        var existingAdmin = await dbContext.Users.FirstOrDefaultAsync(
            x => x.Realm == AccountRealm.Admin && x.Username == normalized,
            cancellationToken);

        if (existingAdmin is not null)
        {
            if (!existingAdmin.IsSuperUser)
            {
                existingAdmin.IsSuperUser = true;
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            return;
        }

        InputValidationHelper.EnsureMinLength(password, 8, "Password must be at least 8 characters long.");

        var domainAdmin = AccountUser.CreateAdmin(normalized, PasswordHasher.HashPassword(password));
        var entity = AccountUserEntityMapper.ToEntity(domainAdmin);
        entity.IsSuperUser = true;
        dbContext.Users.Add(entity);

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
