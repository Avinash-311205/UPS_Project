using System.ComponentModel.DataAnnotations;
using Backend.Common.Interfaces;
using Backend.Modules.Dependencies.Models;
using Backend.Modules.Operations.Models;
using Backend.Modules.Systems.Enums;
using Backend.Modules.Tenants.Models;

namespace Backend.Modules.Systems.Models
{
    public class LogisticsSystem : ITenantEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid TenantId { get; set; }

        public Tenant? Tenant { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public SystemType SystemType { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        [MaxLength(500)]
        public string? BusinessPurpose { get; set; }

        public SystemCriticality Criticality { get; set; }

        public SystemEnvironment Environment { get; set; }

        public SystemLifecycleStatus LifecycleStatus { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Systems that this system depends on (upstream dependencies).
        /// </summary>
        public ICollection<SystemDependency> Dependencies { get; set; } = new List<SystemDependency>();

        /// <summary>
        /// Systems that depend on this system (downstream dependents).
        /// </summary>
        public ICollection<SystemDependency> Dependents { get; set; } = new List<SystemDependency>();

        /// <summary>
        /// Operations that this system supports.
        /// </summary>
        public ICollection<SystemBusinessOperation> SupportedOperations { get; set; } = new List<SystemBusinessOperation>();

        /// <summary>
        /// Incidents associated with this system.
        /// </summary>
        public ICollection<Backend.Modules.Incidents.Models.Incident> Incidents { get; set; } = new List<Backend.Modules.Incidents.Models.Incident>();
    }
}
