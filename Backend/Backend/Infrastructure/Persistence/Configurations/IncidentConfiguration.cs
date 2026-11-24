using Backend.Modules.Incidents.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.Infrastructure.Persistence.Configurations
{
    public class IncidentConfiguration : IEntityTypeConfiguration<Incident>
    {
        public void Configure(EntityTypeBuilder<Incident> builder)
        {
            builder.ToTable("Incidents");

            builder.HasKey(i => i.Id);

            builder.Property(i => i.TenantId)
                .IsRequired();

            builder.Property(i => i.FailedSystemId)
                .IsRequired();

            builder.Property(i => i.Title)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(i => i.Description)
                .HasMaxLength(2000);

            builder.Property(i => i.IncidentType)
                .IsRequired()
                .HasConversion<int>();

            builder.Property(i => i.Severity)
                .IsRequired()
                .HasConversion<int>();

            builder.Property(i => i.Status)
                .IsRequired()
                .HasConversion<int>();

            builder.Property(i => i.DetectedAt)
                .IsRequired();

            builder.Property(i => i.CreatedAt)
                .IsRequired();

            builder.Property(i => i.UpdatedAt);

            builder.Property(i => i.ResolvedAt);

            // Foreign keys
            builder.HasOne(i => i.Tenant)
                .WithMany(t => t.Incidents)
                .HasForeignKey(i => i.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(i => i.FailedSystem)
                .WithMany(s => s.Incidents)
                .HasForeignKey(i => i.FailedSystemId)
                .OnDelete(DeleteBehavior.Restrict);

            // Tenant-scoped query performance indexes
            builder.HasIndex(i => i.TenantId);
            builder.HasIndex(i => new { i.TenantId, i.Status });
            builder.HasIndex(i => new { i.TenantId, i.Severity });
            builder.HasIndex(i => new { i.TenantId, i.FailedSystemId });
            builder.HasIndex(i => new { i.TenantId, i.CreatedAt });
        }
    }
}
