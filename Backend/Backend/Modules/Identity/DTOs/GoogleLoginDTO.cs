using System.ComponentModel.DataAnnotations;

namespace Backend.Modules.Identity.DTOs
{
    public class GoogleLoginDTO
    {
        [Required]
        public string Credential { get; set; } = string.Empty;
    }
}
