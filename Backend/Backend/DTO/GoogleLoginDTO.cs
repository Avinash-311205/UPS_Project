using System.ComponentModel.DataAnnotations;

namespace Backend.DTO
{
    public class GoogleLoginDTO
    {
        [Required]
        public string Credential { get; set; } = string.Empty;
    }
}
