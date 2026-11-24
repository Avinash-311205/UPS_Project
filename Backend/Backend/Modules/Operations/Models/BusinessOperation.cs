using System.ComponentModel.DataAnnotations;
using Backend.Common.Interfaces;
using Backend.Modules.Operations.Enums;
using Backend.Modules.Tenants.Models;

namespace Backend.Modules.Operations.Models
{
    public class BusinessOperation : ITenantEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid TenantId { get; set; }

        public Tenant? Tenant { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        public OperationLifecycleStatus LifecycleStatus { get; set; } = OperationLifecycleStatus.Active;

        public OperationCriticality Criticality { get; set; } = OperationCriticality.Medium;

        public AvailabilityRule AvailabilityRule { get; set; } = AvailabilityRule.AllRequired;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Systems that support this business operation.
        /// </summary>
        public ICollection<SystemBusinessOperation> SupportingSystems { get; set; } = new List<SystemBusinessOperation>();
    }
}
