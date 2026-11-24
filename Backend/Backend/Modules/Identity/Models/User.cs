using System.ComponentModel.DataAnnotations;
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
    }
}
