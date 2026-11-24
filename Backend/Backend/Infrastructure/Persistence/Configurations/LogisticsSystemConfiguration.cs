using Backend.Modules.Systems.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.Infrastructure.Persistence.Configurations
{
    public class LogisticsSystemConfiguration : IEntityTypeConfiguration<LogisticsSystem>
    {
        public void Configure(EntityTypeBuilder<LogisticsSystem> builder)
        {
            builder.ToTable("LogisticsSystems");

            builder.HasKey(s => s.Id);

            builder.Property(s => s.TenantId)
                .IsRequired();

            builder.Property(s => s.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(s => s.SystemType)
                .IsRequired()
                .HasConversion<int>();

            builder.Property(s => s.Description)
                .HasMaxLength(500);

            builder.Property(s => s.BusinessPurpose)
                .HasMaxLength(500);

            builder.Property(s => s.Criticality)
                .IsRequired()
                .HasConversion<int>();

            builder.Property(s => s.Environment)
                .IsRequired()
                .HasConversion<int>();

            builder.Property(s => s.LifecycleStatus)
                .IsRequired()
                .HasConversion<int>();

            builder.Property(s => s.CreatedAt)
                .IsRequired();

            builder.Property(s => s.UpdatedAt);

            builder.HasOne(s => s.Tenant)
                .WithMany(t => t.Systems)
                .HasForeignKey(s => s.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(s => s.Dependencies)
                .WithOne(d => d.DependentSystem)
                .HasForeignKey(d => d.DependentSystemId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(s => s.Dependents)
                .WithOne(d => d.DependencySystem)
                .HasForeignKey(d => d.DependencySystemId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes for tenant-scoped query performance (non-unique to allow multiple systems per type)
            builder.HasIndex(s => s.TenantId);
            builder.HasIndex(s => new { s.TenantId, s.SystemType });
            builder.HasIndex(s => new { s.TenantId, s.LifecycleStatus });
            builder.HasIndex(s => new { s.TenantId, s.Name });
        }
    }
}
