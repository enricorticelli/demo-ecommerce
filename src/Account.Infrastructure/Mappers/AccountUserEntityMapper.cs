using Account.Domain.Entities;
using Account.Infrastructure.Persistence.Entities;

namespace Account.Infrastructure.Mappers;

internal static class AccountUserEntityMapper
{
    public static AccountUser ToDomain(AccountUserEntity entity)
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

    public static AccountUserEntity ToEntity(AccountUser domain)
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

    public static void ApplyDomain(AccountUser domain, AccountUserEntity entity)
    {
        entity.PasswordHash = domain.PasswordHash;
        entity.IsEmailVerified = domain.IsEmailVerified;
        entity.FirstName = domain.FirstName;
        entity.LastName = domain.LastName;
        entity.Phone = domain.Phone;
    }
}
