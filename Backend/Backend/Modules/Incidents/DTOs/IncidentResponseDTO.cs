using Backend.Modules.Incidents.Enums;
using Backend.Modules.Systems.Enums;

namespace Backend.Modules.Incidents.DTOs
{
    public class IncidentResponseDTO
    {
        public Guid Id { get; set; }

        public Guid TenantId { get; set; }

        public Guid FailedSystemId { get; set; }

        public string FailedSystemName { get; set; } = string.Empty;

        public SystemCriticality FailedSystemCriticality { get; set; }

        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        public IncidentType IncidentType { get; set; }

        public IncidentSeverity Severity { get; set; }

        public IncidentStatus Status { get; set; }

        public DateTime DetectedAt { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public DateTime? ResolvedAt { get; set; }
    }
}
