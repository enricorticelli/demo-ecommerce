using Communication.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Communication.Infrastructure.Persistence;

public sealed class CommunicationDbContext(DbContextOptions<CommunicationDbContext> options) : DbContext(options)
{
    public DbSet<ProcessedCommunicationIntegrationEvent> ProcessedIntegrationEvents => Set<ProcessedCommunicationIntegrationEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("communication");

        modelBuilder.Entity<ProcessedCommunicationIntegrationEvent>(entity =>
        {
            entity.ToTable("processed_integration_events");
            entity.HasKey(x => x.EventId);
            entity.Property(x => x.ProcessedAtUtc).IsRequired();
        });

        base.OnModelCreating(modelBuilder);
    }
}
