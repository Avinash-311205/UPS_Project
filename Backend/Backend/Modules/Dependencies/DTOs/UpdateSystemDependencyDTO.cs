using System.ComponentModel.DataAnnotations;
using Backend.Modules.Dependencies.Enums;

namespace Backend.Modules.Dependencies.DTOs
{
    public class UpdateSystemDependencyDTO
    {
        [Required(ErrorMessage = "DependencyType is required.")]
        public DependencyType DependencyType { get; set; }
    }
}
