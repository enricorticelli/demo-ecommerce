using System;
using Communication.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Communication.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(CommunicationDbContext))]
    partial class CommunicationDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasDefaultSchema("communication")
                .HasAnnotation("ProductVersion", "10.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("Communication.Infrastructure.Persistence.Entities.ProcessedCommunicationIntegrationEvent", b =>
                {
                    b.Property<Guid>("EventId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<DateTimeOffset>("ProcessedAtUtc")
                        .HasColumnType("timestamp with time zone");

                    b.HasKey("EventId");

                    b.ToTable("processed_integration_events", "communication");
                });
#pragma warning restore 612, 618
        }
    }
}
