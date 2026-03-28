using Microsoft.EntityFrameworkCore;
using Warehouse.Infrastructure.Persistence.Entities;

namespace Warehouse.Infrastructure.Persistence;

public sealed class WarehouseDbContext(DbContextOptions<WarehouseDbContext> options) : DbContext(options)
{
    public DbSet<Warehouse.Domain.Entities.WarehouseStockItem> WarehouseStockItems => Set<Warehouse.Domain.Entities.WarehouseStockItem>();
    public DbSet<Warehouse.Domain.Entities.WarehouseReservation> WarehouseReservations => Set<Warehouse.Domain.Entities.WarehouseReservation>();
    public DbSet<ProcessedWarehouseIntegrationEvent> ProcessedIntegrationEvents => Set<ProcessedWarehouseIntegrationEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("warehouse");

        modelBuilder.Entity<Warehouse.Domain.Entities.WarehouseStockItem>(entity =>
        {
            entity.ToTable("warehouse_stock_items");
            entity.HasKey(x => x.ProductId);

            entity.Property(x => x.Sku).HasMaxLength(64).IsRequired();
            entity.Property(x => x.AvailableQuantity).IsRequired();
            entity.Property(x => x.UpdatedAtUtc).IsRequired();

            entity.HasIndex(x => x.Sku).IsUnique();
        });

        modelBuilder.Entity<Warehouse.Domain.Entities.WarehouseReservation>(entity =>
        {
            entity.ToTable("warehouse_reservations");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.OrderId).IsRequired();
            entity.Property(x => x.TotalAmount).HasPrecision(18, 2).IsRequired();
            entity.Property(x => x.IsReserved).IsRequired();
            entity.Property(x => x.FailureReason).HasMaxLength(256).IsRequired(false);
            entity.Property(x => x.CreatedAtUtc).IsRequired();

            entity.HasIndex(x => x.OrderId).IsUnique();
        });

        modelBuilder.Entity<ProcessedWarehouseIntegrationEvent>(entity =>
        {
            entity.ToTable("processed_integration_events");
            entity.HasKey(x => x.EventId);
            entity.Property(x => x.ProcessedAtUtc).IsRequired();
        });

        base.OnModelCreating(modelBuilder);
    }
}
