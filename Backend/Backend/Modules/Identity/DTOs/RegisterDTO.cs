using System.ComponentModel.DataAnnotations;

namespace Backend.Modules.Identity.DTOs
{
    public class RegisterDTO
    {
        [Required]
        [MaxLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(254)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(8)]
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// Optional: Name of a new organization/tenant workspace to create.
        /// Client cannot supply a TenantId to prevent arbitrary tenant assignment.
        /// </summary>
        [MaxLength(100)]
        public string? TenantName { get; set; }
    }
}
