using System.ComponentModel.DataAnnotations;

namespace Backend.DTO
{
    public class RegisterDTO
    {
        [Required]
        [MaxLength(10)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(254)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(8)]
        public string Password { get; set; } = string.Empty;
    }
}
