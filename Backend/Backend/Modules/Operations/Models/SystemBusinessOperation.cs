using Backend.Common.Interfaces;
using Backend.Modules.Systems.Models;
using Backend.Modules.Tenants.Models;

namespace Backend.Modules.Operations.Models
{
    public class SystemBusinessOperation : ITenantEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid TenantId { get; set; }

        public Tenant? Tenant { get; set; }

        public Guid LogisticsSystemId { get; set; }

        public LogisticsSystem? LogisticsSystem { get; set; }

        public Guid BusinessOperationId { get; set; }

        public BusinessOperation? BusinessOperation { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
