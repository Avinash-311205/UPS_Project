using Backend.Modules.Identity.Enums;

namespace Backend.Modules.IAM.DTOs
{
    public class IamUserResponseDTO
    {
        public int Id { get; set; }

        public string Username { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public Guid? TenantId { get; set; }

        public AccountType AccountType { get; set; }

        public UserStatus Status { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }
    }
}
