using Account.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Account.Infrastructure.Persistence;

public sealed class AccountDbContext(DbContextOptions<AccountDbContext> options) : DbContext(options)
{
    public DbSet<AccountUserEntity> Users => Set<AccountUserEntity>();
    public DbSet<CustomerAddressEntity> Addresses => Set<CustomerAddressEntity>();
    public DbSet<RefreshSessionEntity> RefreshSessions => Set<RefreshSessionEntity>();
    public DbSet<EmailVerificationTokenEntity> EmailVerificationTokens => Set<EmailVerificationTokenEntity>();
    public DbSet<PasswordResetTokenEntity> PasswordResetTokens => Set<PasswordResetTokenEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("accounting");

        modelBuilder.Entity<AccountUserEntity>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Realm).HasMaxLength(32).IsRequired();
            entity.Property(x => x.Username).HasMaxLength(128).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(256).IsRequired();
            entity.Property(x => x.NormalizedEmail).HasMaxLength(256).IsRequired();
            entity.Property(x => x.PasswordHash).HasMaxLength(512).IsRequired();
            entity.Property(x => x.FirstName).HasMaxLength(128).IsRequired();
            entity.Property(x => x.LastName).HasMaxLength(128).IsRequired();
            entity.Property(x => x.Phone).HasMaxLength(64).IsRequired();
            entity.Property(x => x.IsSuperUser).HasDefaultValue(false).IsRequired();
            entity.Property(x => x.CustomPermissions).HasColumnType("text[]");
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.HasIndex(x => new { x.Realm, x.NormalizedEmail }).IsUnique();
            entity.HasIndex(x => new { x.Realm, x.Username }).IsUnique();
        });

        modelBuilder.Entity<CustomerAddressEntity>(entity =>
        {
            entity.ToTable("customer_addresses");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Label).HasMaxLength(64).IsRequired();
            entity.Property(x => x.Street).HasMaxLength(256).IsRequired();
            entity.Property(x => x.City).HasMaxLength(128).IsRequired();
            entity.Property(x => x.PostalCode).HasMaxLength(32).IsRequired();
            entity.Property(x => x.Country).HasMaxLength(64).IsRequired();
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.HasIndex(x => x.UserId);
        });

        modelBuilder.Entity<RefreshSessionEntity>(entity =>
        {
            entity.ToTable("refresh_sessions");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Realm).HasMaxLength(32).IsRequired();
            entity.Property(x => x.TokenHash).HasMaxLength(128).IsRequired();
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.Property(x => x.ExpiresAtUtc).IsRequired();
            entity.HasIndex(x => x.TokenHash).IsUnique();
            entity.HasIndex(x => new { x.UserId, x.Realm });
        });

        modelBuilder.Entity<EmailVerificationTokenEntity>(entity =>
        {
            entity.ToTable("email_verification_tokens");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.CodeHash).HasMaxLength(128).IsRequired();
            entity.Property(x => x.ExpiresAtUtc).IsRequired();
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.HasIndex(x => x.UserId);
        });

        modelBuilder.Entity<PasswordResetTokenEntity>(entity =>
        {
            entity.ToTable("password_reset_tokens");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.CodeHash).HasMaxLength(128).IsRequired();
            entity.Property(x => x.ExpiresAtUtc).IsRequired();
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.HasIndex(x => x.UserId);
        });

        base.OnModelCreating(modelBuilder);
    }
}
