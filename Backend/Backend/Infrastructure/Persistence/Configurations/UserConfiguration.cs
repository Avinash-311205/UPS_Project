using Backend.Modules.Identity.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.Infrastructure.Persistence.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("Users");

            builder.HasKey(u => u.Id);

            builder.Property(u => u.Id)
                .ValueGeneratedOnAdd();

            builder.Property(u => u.Username)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(u => u.Email)
                .IsRequired()
                .HasMaxLength(254);

            builder.Property(u => u.PasswordHash)
                .IsRequired();

            builder.Property(u => u.TenantId)
                .IsRequired(false);

            builder.Property(u => u.AccountType)
                .IsRequired()
                .HasDefaultValue(Backend.Modules.Identity.Enums.AccountType.IAM);

            builder.Property(u => u.Status)
                .IsRequired()
                .HasDefaultValue(Backend.Modules.Identity.Enums.UserStatus.Active);

            builder.Property(u => u.SecurityStamp)
                .IsRequired()
                .HasMaxLength(36);

            builder.Property(u => u.CreatedAt)
                .IsRequired();

            builder.Property(u => u.UpdatedAt)
                .IsRequired(false);

            builder.Property(u => u.RootTenantId)
                .IsRequired(false)
                .ValueGeneratedOnAddOrUpdate();

            builder.HasIndex(u => u.RootTenantId)
                .IsUnique();

            builder.HasOne(u => u.Tenant)
                .WithMany(t => t.Users)
                .HasForeignKey(u => u.TenantId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
