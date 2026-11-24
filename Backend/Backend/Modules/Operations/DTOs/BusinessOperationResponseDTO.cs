using Backend.Modules.Operations.Enums;

namespace Backend.Modules.Operations.DTOs
{
    public class BusinessOperationResponseDTO
    {
        public Guid Id { get; set; }

        public Guid TenantId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public OperationLifecycleStatus LifecycleStatus { get; set; }

        public OperationCriticality Criticality { get; set; }

        public AvailabilityRule AvailabilityRule { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public int SupportingSystemsCount { get; set; }
    }
}
