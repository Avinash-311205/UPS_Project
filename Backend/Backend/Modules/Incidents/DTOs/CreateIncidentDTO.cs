using System.ComponentModel.DataAnnotations;
using Backend.Modules.Incidents.Enums;

namespace Backend.Modules.Incidents.DTOs
{
    public class CreateIncidentDTO
    {
        [Required(ErrorMessage = "FailedSystemId is required.")]
        public Guid FailedSystemId { get; set; }

        [Required(ErrorMessage = "Title is required.")]
        [MaxLength(200, ErrorMessage = "Title cannot exceed 200 characters.")]
        public string Title { get; set; } = string.Empty;

        [MaxLength(2000, ErrorMessage = "Description cannot exceed 2000 characters.")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "IncidentType is required.")]
        public IncidentType IncidentType { get; set; }

        [Required(ErrorMessage = "Severity is required.")]
        public IncidentSeverity Severity { get; set; }

        public DateTime? DetectedAt { get; set; }
    }
}
