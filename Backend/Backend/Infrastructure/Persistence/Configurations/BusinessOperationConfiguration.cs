using Backend.Modules.Operations.Enums;
using Backend.Modules.Operations.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.Infrastructure.Persistence.Configurations
{
    public class BusinessOperationConfiguration : IEntityTypeConfiguration<BusinessOperation>
    {
        public void Configure(EntityTypeBuilder<BusinessOperation> builder)
        {
            builder.ToTable("BusinessOperations");

            builder.HasKey(o => o.Id);

            builder.Property(o => o.TenantId)
                .IsRequired()
                .HasColumnType("char(36)");

            builder.Property(o => o.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(o => o.Description)
                .HasMaxLength(500);

            builder.Property(o => o.LifecycleStatus)
                .IsRequired()
                .HasConversion<int>();

            builder.Property(o => o.Criticality)
                .IsRequired()
                .HasConversion<int>()
                .HasDefaultValue(OperationCriticality.Medium);

            builder.Property(o => o.AvailabilityRule)
                .IsRequired()
                .HasConversion<int>()
                .HasDefaultValue(AvailabilityRule.AllRequired);

            builder.Property(o => o.CreatedAt)
                .IsRequired();

            builder.Property(o => o.UpdatedAt);

            // Foreign key to Tenant
            builder.HasOne(o => o.Tenant)
                .WithMany(t => t.BusinessOperations)
                .HasForeignKey(o => o.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            // Operation names must be unique within a tenant
            builder.HasIndex(o => new { o.TenantId, o.Name })
                .IsUnique();

            builder.HasIndex(o => new { o.TenantId, o.LifecycleStatus });
            builder.HasIndex(o => new { o.TenantId, o.Criticality });
            builder.HasIndex(o => new { o.TenantId, o.AvailabilityRule });
        }
    }
}
