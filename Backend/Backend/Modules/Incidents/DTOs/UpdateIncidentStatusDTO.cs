using System.ComponentModel.DataAnnotations;
using Backend.Modules.Incidents.Enums;

namespace Backend.Modules.Incidents.DTOs
{
    public class UpdateIncidentStatusDTO
    {
        [Required(ErrorMessage = "Status is required.")]
        public IncidentStatus Status { get; set; }

        [MaxLength(2000, ErrorMessage = "Resolution notes cannot exceed 2000 characters.")]
        public string? ResolutionNotes { get; set; }
    }
}
