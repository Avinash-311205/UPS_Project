using System.ComponentModel.DataAnnotations;
using Backend.Modules.Identity.Enums;
using Backend.Modules.Tenants.Models;

namespace Backend.Modules.Identity.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(254)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        /// <summary>
        /// Temporarily nullable to safely support pre-existing user accounts without inventing tenant assignments.
        /// Core domain rule: Every active user must belong to exactly one Tenant.
        /// </summary>
        public Guid? TenantId { get; set; }

        public Tenant? Tenant { get; set; }

        public AccountType AccountType { get; set; } = AccountType.IAM;

        public UserStatus Status { get; set; } = UserStatus.Active;

        [Required]
        [MaxLength(36)]
        public string SecurityStamp { get; set; } = Guid.NewGuid().ToString();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Generated column: Equals TenantId when AccountType is Root, NULL when IAM.
        /// Enforced by MySQL UNIQUE INDEX UX_Users_RootTenantId to guarantee one Root per Tenant.
        /// </summary>
        public Guid? RootTenantId { get; set; }
    }
}
