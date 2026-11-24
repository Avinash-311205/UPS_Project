using Backend.Modules.Dependencies.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.Infrastructure.Persistence.Configurations
{
    public class SystemDependencyConfiguration : IEntityTypeConfiguration<SystemDependency>
    {
        public void Configure(EntityTypeBuilder<SystemDependency> builder)
        {
            builder.ToTable("SystemDependencies");

            builder.HasKey(d => d.Id);

            builder.Property(d => d.TenantId)
                .IsRequired();

            builder.Property(d => d.DependentSystemId)
                .IsRequired();

            builder.Property(d => d.DependencySystemId)
                .IsRequired();

            builder.Property(d => d.DependencyType)
                .IsRequired()
                .HasConversion<int>();

            builder.Property(d => d.CreatedAt)
                .IsRequired();

            builder.Property(d => d.UpdatedAt);

            // Foreign Key to Tenant: Restrict deletion of tenant
            builder.HasOne(d => d.Tenant)
                .WithMany(t => t.Dependencies)
                .HasForeignKey(d => d.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            // Foreign Key to DependentSystem (the system that has the dependency)
            builder.HasOne(d => d.DependentSystem)
                .WithMany(s => s.Dependencies)
                .HasForeignKey(d => d.DependentSystemId)
                .OnDelete(DeleteBehavior.Restrict);

            // Foreign Key to DependencySystem (the system being depended upon)
            builder.HasOne(d => d.DependencySystem)
                .WithMany(s => s.Dependents)
                .HasForeignKey(d => d.DependencySystemId)
                .OnDelete(DeleteBehavior.Restrict);

            // Critical Uniqueness Rule:
            // Reject duplicate relationships regardless of DependencyType.
            builder.HasIndex(d => new { d.TenantId, d.DependentSystemId, d.DependencySystemId })
                .IsUnique();

            // Tenant-scoped performance query indexes
            builder.HasIndex(d => d.TenantId);
            builder.HasIndex(d => new { d.TenantId, d.DependentSystemId });
            builder.HasIndex(d => new { d.TenantId, d.DependencySystemId });
        }
    }
}
