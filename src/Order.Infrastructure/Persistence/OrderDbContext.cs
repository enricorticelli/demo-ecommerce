using Microsoft.EntityFrameworkCore;
using Order.Domain.ValueObjects;
using Order.Infrastructure.Persistence.Entities;

namespace Order.Infrastructure.Persistence;

public sealed class OrderDbContext(DbContextOptions<OrderDbContext> options) : DbContext(options)
{
    public DbSet<Order.Domain.Entities.Order> Orders => Set<Order.Domain.Entities.Order>();
    public DbSet<ProcessedOrderIntegrationEvent> ProcessedIntegrationEvents => Set<ProcessedOrderIntegrationEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("ordering");

        modelBuilder.Entity<Order.Domain.Entities.Order>(entity =>
        {
            entity.ToTable("orders");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.CartId).IsRequired();
            entity.Property(x => x.UserId).IsRequired();
            entity.Property(x => x.IdentityType).HasMaxLength(32).IsRequired();
            entity.Property(x => x.PaymentMethod).HasMaxLength(64).IsRequired();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.Property(x => x.TotalAmount).HasPrecision(18, 2).IsRequired();
            entity.Property(x => x.TrackingCode).HasMaxLength(128).IsRequired();
            entity.Property(x => x.TransactionId).HasMaxLength(128).IsRequired();
            entity.Property(x => x.FailureReason).HasMaxLength(256).IsRequired();
            entity.Property(x => x.IsPaymentAuthorized).IsRequired();
            entity.Property(x => x.IsStockReserved).IsRequired();
            entity.Property(x => x.CreatedAtUtc).IsRequired();

            entity.OwnsOne(x => x.Customer, customer =>
            {
                customer.Property(x => x.FirstName).HasColumnName("customer_first_name").HasMaxLength(128).IsRequired();
                customer.Property(x => x.LastName).HasColumnName("customer_last_name").HasMaxLength(128).IsRequired();
                customer.Property(x => x.Email).HasColumnName("customer_email").HasMaxLength(256).IsRequired();
                customer.Property(x => x.Phone).HasColumnName("customer_phone").HasMaxLength(64).IsRequired();
            });

            entity.OwnsOne(x => x.ShippingAddress, address =>
            {
                address.Property(x => x.Street).HasColumnName("shipping_street").HasMaxLength(256).IsRequired();
                address.Property(x => x.City).HasColumnName("shipping_city").HasMaxLength(128).IsRequired();
                address.Property(x => x.PostalCode).HasColumnName("shipping_postal_code").HasMaxLength(32).IsRequired();
                address.Property(x => x.Country).HasColumnName("shipping_country").HasMaxLength(64).IsRequired();
            });

            entity.OwnsOne(x => x.BillingAddress, address =>
            {
                address.Property(x => x.Street).HasColumnName("billing_street").HasMaxLength(256).IsRequired();
                address.Property(x => x.City).HasColumnName("billing_city").HasMaxLength(128).IsRequired();
                address.Property(x => x.PostalCode).HasColumnName("billing_postal_code").HasMaxLength(32).IsRequired();
                address.Property(x => x.Country).HasColumnName("billing_country").HasMaxLength(64).IsRequired();
            });

            entity.OwnsMany(x => x.Items, item =>
            {
                item.ToTable("order_items");
                item.WithOwner().HasForeignKey("order_id");
                item.HasKey("order_id", nameof(OrderItem.ProductId));

                item.Property(x => x.ProductId).HasColumnName("product_id").IsRequired();
                item.Property(x => x.Sku).HasColumnName("sku").HasMaxLength(64).IsRequired();
                item.Property(x => x.Name).HasColumnName("name").HasMaxLength(256).IsRequired();
                item.Property(x => x.Quantity).HasColumnName("quantity").IsRequired();
                item.Property(x => x.UnitPrice).HasColumnName("unit_price").HasPrecision(18, 2).IsRequired();
            });
        });

        modelBuilder.Entity<ProcessedOrderIntegrationEvent>(entity =>
        {
            entity.ToTable("processed_integration_events");
            entity.HasKey(x => x.EventId);
            entity.Property(x => x.ProcessedAtUtc).IsRequired();
        });

        base.OnModelCreating(modelBuilder);
    }
}
