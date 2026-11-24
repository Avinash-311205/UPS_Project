using Backend.Modules.Systems.Enums;

namespace Backend.Modules.Systems.DTOs
{
    public class LogisticsSystemResponseDTO
    {
        public Guid Id { get; set; }

        public Guid TenantId { get; set; }

        public string Name { get; set; } = string.Empty;

        public SystemType SystemType { get; set; }

        public string? Description { get; set; }

        public string? BusinessPurpose { get; set; }

        public SystemCriticality Criticality { get; set; }

        public SystemEnvironment Environment { get; set; }

        public SystemLifecycleStatus LifecycleStatus { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }
    }
}
