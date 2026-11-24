using System.ComponentModel.DataAnnotations;
using Backend.Modules.Identity.Enums;

namespace Backend.Modules.IAM.DTOs
{
    public class UpdateIamUserStatusDTO
    {
        [Required(ErrorMessage = "Status is required.")]
        public UserStatus Status { get; set; }
    }
}
