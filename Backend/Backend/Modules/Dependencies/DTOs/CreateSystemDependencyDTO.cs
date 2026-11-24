using System.ComponentModel.DataAnnotations;
using Backend.Modules.Dependencies.Enums;

namespace Backend.Modules.Dependencies.DTOs
{
    public class CreateSystemDependencyDTO
    {
        [Required(ErrorMessage = "DependentSystemId is required.")]
        public Guid DependentSystemId { get; set; }

        [Required(ErrorMessage = "DependencySystemId is required.")]
        public Guid DependencySystemId { get; set; }

        [Required(ErrorMessage = "DependencyType is required.")]
        public DependencyType DependencyType { get; set; }
    }
}
