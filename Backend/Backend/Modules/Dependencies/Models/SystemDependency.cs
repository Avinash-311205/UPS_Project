using Backend.Common.Interfaces;
using Backend.Modules.Dependencies.Enums;
using Backend.Modules.Systems.Models;
using Backend.Modules.Tenants.Models;

namespace Backend.Modules.Dependencies.Models
{
    public class SystemDependency : ITenantEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid TenantId { get; set; }

        public Tenant? Tenant { get; set; }

        public Guid DependentSystemId { get; set; }

        public LogisticsSystem? DependentSystem { get; set; }

        public Guid DependencySystemId { get; set; }

        public LogisticsSystem? DependencySystem { get; set; }

        public DependencyType DependencyType { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}
