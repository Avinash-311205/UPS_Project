using System.ComponentModel.DataAnnotations;
using Backend.Modules.Systems.Enums;

namespace Backend.Modules.Systems.DTOs
{
    public class UpdateLogisticsSystemDTO
    {
        [Required(ErrorMessage = "Name is required.")]
        [MaxLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "SystemType is required.")]
        public SystemType SystemType { get; set; }

        [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        public string? Description { get; set; }

        [MaxLength(500, ErrorMessage = "BusinessPurpose cannot exceed 500 characters.")]
        public string? BusinessPurpose { get; set; }

        [Required(ErrorMessage = "Criticality is required.")]
        public SystemCriticality Criticality { get; set; }

        [Required(ErrorMessage = "Environment is required.")]
        public SystemEnvironment Environment { get; set; }

        [Required(ErrorMessage = "LifecycleStatus is required.")]
        public SystemLifecycleStatus LifecycleStatus { get; set; }
    }
}
