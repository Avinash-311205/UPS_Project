using System.ComponentModel.DataAnnotations;
using Backend.Modules.Operations.Enums;

namespace Backend.Modules.Operations.DTOs
{
    public class CreateBusinessOperationDTO
    {
        [Required(ErrorMessage = "Operation name is required.")]
        [MaxLength(100, ErrorMessage = "Operation name cannot exceed 100 characters.")]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        public string? Description { get; set; }

        public OperationCriticality Criticality { get; set; } = OperationCriticality.Medium;

        public AvailabilityRule AvailabilityRule { get; set; } = AvailabilityRule.AllRequired;
    }
}
