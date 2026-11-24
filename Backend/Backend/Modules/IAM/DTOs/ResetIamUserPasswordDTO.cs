using System.ComponentModel.DataAnnotations;

namespace Backend.Modules.IAM.DTOs
{
    public class ResetIamUserPasswordDTO
    {
        [Required(ErrorMessage = "New password is required.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters long.")]
        public string NewPassword { get; set; } = string.Empty;
    }
}
