using System.ComponentModel.DataAnnotations;
using Backend.Common.Interfaces;
using Backend.Modules.Incidents.Enums;
using Backend.Modules.Systems.Models;
using Backend.Modules.Tenants.Models;

namespace Backend.Modules.Incidents.Models
{
    public class Incident : ITenantEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid TenantId { get; set; }

        public Tenant? Tenant { get; set; }

        public Guid FailedSystemId { get; set; }

        public LogisticsSystem? FailedSystem { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? Description { get; set; }

        public IncidentType IncidentType { get; set; }

        public IncidentSeverity Severity { get; set; }

        public IncidentStatus Status { get; set; } = IncidentStatus.Open;

        public DateTime DetectedAt { get; set; } = DateTime.UtcNow;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public DateTime? ResolvedAt { get; set; }
    }
}
