using Microsoft.EntityFrameworkCore;

namespace Cart.Infrastructure.Persistence;

public sealed class CartDbContext(DbContextOptions<CartDbContext> options) : DbContext(options)
{
    public DbSet<Cart.Domain.Entities.Cart> Carts => Set<Cart.Domain.Entities.Cart>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("cart");

        modelBuilder.Entity<Cart.Domain.Entities.Cart>(entity =>
        {
            entity.ToTable("carts");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.UserId).IsRequired();
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.Property(x => x.UpdatedAtUtc).IsRequired();

            entity.OwnsMany(x => x.Items, items =>
            {
                items.ToTable("cart_items");
                items.WithOwner().HasForeignKey("CartId");
                items.Property<Guid>("Id");
                items.HasKey("Id");
                items.Property(x => x.ProductId).IsRequired();
                items.Property(x => x.Sku).HasMaxLength(64).IsRequired();
                items.Property(x => x.Name).HasMaxLength(256).IsRequired();
                items.Property(x => x.Quantity).IsRequired();
                items.Property(x => x.UnitPrice).HasPrecision(18, 2).IsRequired();
                items.HasIndex(x => x.ProductId);
            });
        });

        base.OnModelCreating(modelBuilder);
    }
}
