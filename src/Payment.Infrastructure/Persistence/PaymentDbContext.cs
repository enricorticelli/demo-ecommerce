using Microsoft.EntityFrameworkCore;
using Payment.Infrastructure.Persistence.Entities;

namespace Payment.Infrastructure.Persistence;

public sealed class PaymentDbContext(DbContextOptions<PaymentDbContext> options) : DbContext(options)
{
    public DbSet<Payment.Domain.Entities.PaymentSession> PaymentSessions => Set<Payment.Domain.Entities.PaymentSession>();
    public DbSet<ProcessedPaymentIntegrationEvent> ProcessedIntegrationEvents => Set<ProcessedPaymentIntegrationEvent>();
    public DbSet<ProcessedPaymentWebhookEvent> ProcessedWebhookEvents => Set<ProcessedPaymentWebhookEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("payment");

        modelBuilder.Entity<Payment.Domain.Entities.PaymentSession>(entity =>
        {
            entity.ToTable("payment_sessions");
            entity.HasKey(x => x.SessionId);

            entity.Property(x => x.OrderId).IsRequired();
            entity.Property(x => x.UserId).IsRequired();
            entity.Property(x => x.Amount).HasPrecision(18, 2).IsRequired();
            entity.Property(x => x.PaymentMethod).HasMaxLength(64).IsRequired();
            entity.Property(x => x.ProviderCode).HasMaxLength(32).IsRequired();
            entity.Property(x => x.ExternalCheckoutId).HasMaxLength(128).IsRequired();
            entity.Property(x => x.ProviderStatus).HasMaxLength(64).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(32).IsRequired();
            entity.Property(x => x.TransactionId).HasMaxLength(128).IsRequired(false);
            entity.Property(x => x.FailureReason).HasMaxLength(256).IsRequired(false);
            entity.Property(x => x.LastWebhookEventId).HasMaxLength(128).IsRequired();
            entity.Property(x => x.LastProviderPayload).HasMaxLength(4096).IsRequired();
            entity.Property(x => x.LastWebhookReceivedAtUtc).IsRequired(false);
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.Property(x => x.CompletedAtUtc).IsRequired(false);
            entity.Property(x => x.RedirectUrl).HasMaxLength(512).IsRequired();

            entity.HasIndex(x => x.OrderId).IsUnique();
            entity.HasIndex(x => x.ExternalCheckoutId);
        });

        modelBuilder.Entity<ProcessedPaymentIntegrationEvent>(entity =>
        {
            entity.ToTable("processed_integration_events");
            entity.HasKey(x => x.EventId);
            entity.Property(x => x.ProcessedAtUtc).IsRequired();
        });

        modelBuilder.Entity<ProcessedPaymentWebhookEvent>(entity =>
        {
            entity.ToTable("processed_webhook_events");
            entity.HasKey(x => new { x.ProviderCode, x.ExternalEventId });
            entity.Property(x => x.ProviderCode).HasMaxLength(32).IsRequired();
            entity.Property(x => x.ExternalEventId).HasMaxLength(128).IsRequired();
            entity.Property(x => x.ProcessedAtUtc).IsRequired();
        });

        base.OnModelCreating(modelBuilder);
    }
}
