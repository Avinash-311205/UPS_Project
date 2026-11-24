using Backend.Modules.Dependencies.Enums;
using Backend.Modules.Systems.DTOs;

namespace Backend.Modules.Dependencies.DTOs
{
    public class SystemDependencyResponseDTO
    {
        public Guid Id { get; set; }

        public Guid TenantId { get; set; }

        public Guid DependentSystemId { get; set; }

        public LogisticsSystemResponseDTO? DependentSystem { get; set; }

        public Guid DependencySystemId { get; set; }

        public LogisticsSystemResponseDTO? DependencySystem { get; set; }

        public DependencyType DependencyType { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }
    }
}
