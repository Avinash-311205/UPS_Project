namespace Backend.Modules.Operations.DTOs
{
    public class SystemBusinessOperationResponseDTO
    {
        public Guid Id { get; set; }

        public Guid TenantId { get; set; }

        public Guid LogisticsSystemId { get; set; }

        public string LogisticsSystemName { get; set; } = string.Empty;

        public Guid BusinessOperationId { get; set; }

        public string BusinessOperationName { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
    }
}
