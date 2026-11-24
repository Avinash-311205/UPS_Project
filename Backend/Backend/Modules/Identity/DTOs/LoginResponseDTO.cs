using Backend.Modules.Identity.Enums;

namespace Backend.Modules.Identity.DTOs
{
    public class LoginResponseDTO
    {
        public string Token { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public Guid? TenantId { get; set; }
        public AccountType AccountType { get; set; } = AccountType.IAM;
        public UserStatus Status { get; set; } = UserStatus.Active;
    }
}
