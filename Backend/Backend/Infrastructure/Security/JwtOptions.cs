namespace Backend.Infrastructure.Security
{
    public class JwtOptions
    {
        public const string SectionName = "Jwt";

        public string Key { get; set; } = string.Empty;
        public string? Issuer { get; set; }
        public string? Audience { get; set; }
        public double ExpiryMinutes { get; set; } = 60;
    }
}
