using Backend.Modules.Operations.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.Infrastructure.Persistence.Configurations
{
    public class SystemBusinessOperationConfiguration : IEntityTypeConfiguration<SystemBusinessOperation>
    {
        public void Configure(EntityTypeBuilder<SystemBusinessOperation> builder)
        {
            builder.ToTable("SystemBusinessOperations");

            builder.HasKey(sbo => sbo.Id);

            builder.Property(sbo => sbo.TenantId)
                .IsRequired()
                .HasColumnType("char(36)");

            builder.Property(sbo => sbo.LogisticsSystemId)
                .IsRequired()
                .HasColumnType("char(36)");

            builder.Property(sbo => sbo.BusinessOperationId)
                .IsRequired()
                .HasColumnType("char(36)");

            builder.Property(sbo => sbo.CreatedAt)
                .IsRequired();

            // Foreign keys with Restrict
            builder.HasOne(sbo => sbo.Tenant)
                .WithMany(t => t.SystemBusinessOperations)
                .HasForeignKey(sbo => sbo.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(sbo => sbo.LogisticsSystem)
                .WithMany(s => s.SupportedOperations)
                .HasForeignKey(sbo => sbo.LogisticsSystemId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(sbo => sbo.BusinessOperation)
                .WithMany(o => o.SupportingSystems)
                .HasForeignKey(sbo => sbo.BusinessOperationId)
                .OnDelete(DeleteBehavior.Restrict);

            // Critical composite unique index to prevent duplicate relationships
            builder.HasIndex(sbo => new { sbo.TenantId, sbo.LogisticsSystemId, sbo.BusinessOperationId })
                .IsUnique();

            builder.HasIndex(sbo => sbo.LogisticsSystemId);
            builder.HasIndex(sbo => sbo.BusinessOperationId);
            builder.HasIndex(sbo => sbo.TenantId);
        }
    }
}
