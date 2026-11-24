using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Backend.Modules.Identity.DTOs;
using Backend.Modules.Identity.Interfaces;
using Backend.Modules.Identity.Models;
using Backend.Modules.Tenants.Interfaces;
using Backend.Modules.Tenants.Models;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace Backend.Modules.Identity.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly ITenantRepository _tenantRepository;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly IConfiguration _configuration;

        public AuthService(
            IUserRepository userRepository,
            ITenantRepository tenantRepository,
            IPasswordHasher<User> passwordHasher,
            IConfiguration configuration)
        {
            _userRepository = userRepository;
            _tenantRepository = tenantRepository;
            _passwordHasher = passwordHasher;
            _configuration = configuration;
        }

        public async Task<User?> RegisterAsync(RegisterDTO registerDto, CancellationToken cancellationToken = default)
        {
            // Check if email already exists
            if (await _userRepository.EmailExistsAsync(registerDto.Email, cancellationToken))
            {
                return null;
            }

            // Determine Tenant: Registration always provisions a new dedicated tenant workspace for the user.
            // Client-supplied TenantId is not accepted to prevent arbitrary tenant membership injection.
            var tenantName = !string.IsNullOrWhiteSpace(registerDto.TenantName)
                ? registerDto.TenantName.Trim()
                : $"{registerDto.Username.Trim()}'s Workspace";

            var newTenant = new Tenant
            {
                Id = Guid.NewGuid(),
                Name = tenantName,
                Status = "Active",
                CreatedAt = DateTime.UtcNow
            };
            await _tenantRepository.AddAsync(newTenant, cancellationToken);
            var tenantId = newTenant.Id;

            // Create user entity as Root Account for the newly registered tenant workspace
            var user = new User
            {
                Username = registerDto.Username.Trim(),
                Email = registerDto.Email.Trim(),
                TenantId = tenantId,
                AccountType = Backend.Modules.Identity.Enums.AccountType.Root,
                Status = Backend.Modules.Identity.Enums.UserStatus.Active,
                SecurityStamp = Guid.NewGuid().ToString(),
                CreatedAt = DateTime.UtcNow
            };

            // Hash password and store only the hash
            user.PasswordHash = _passwordHasher.HashPassword(user, registerDto.Password);

            // Persist user via repository
            var createdUser = await _userRepository.AddAsync(user, cancellationToken);

            // Ensure PasswordHash is not exposed in returned entity
            createdUser.PasswordHash = string.Empty;

            return createdUser;
        }

        public async Task<(User User, string Token)?> LoginAsync(LoginDTO loginDto, CancellationToken cancellationToken = default)
        {
            // Retrieve user by email
            var user = await _userRepository.GetByEmailAsync(loginDto.Email, cancellationToken);
            if (user == null)
            {
                return null;
            }

            // Deactivated accounts are prohibited from authenticating
            if (user.Status != Backend.Modules.Identity.Enums.UserStatus.Active)
            {
                return null;
            }

            // Verify password against stored hash
            var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, loginDto.Password);
            if (verificationResult == PasswordVerificationResult.Failed)
            {
                return null;
            }

            // Safe, idempotent tenant onboarding for legacy users without a TenantId
            await EnsureTenantAssignedAsync(user, cancellationToken);

            // Generate and return signed JWT token
            return (user, GenerateJwtToken(user));
        }

        public async Task<(User User, string Token)?> LoginWithGoogleAsync(
            string credential,
            CancellationToken cancellationToken = default)
        {
            var clientId = _configuration["Google:ClientId"];
            if (string.IsNullOrWhiteSpace(clientId))
            {
                throw new InvalidOperationException("Google ClientId is not configured in application settings.");
            }

            GoogleJsonWebSignature.Payload payload;
            try
            {
                payload = await GoogleJsonWebSignature.ValidateAsync(
                    credential,
                    new GoogleJsonWebSignature.ValidationSettings { Audience = new[] { clientId } });
            }
            catch (InvalidJwtException)
            {
                return null;
            }

            if (payload.EmailVerified != true || string.IsNullOrWhiteSpace(payload.Email))
            {
                return null;
            }

            var email = payload.Email.Trim();
            var user = await _userRepository.GetByEmailAsync(email, cancellationToken);
            if (user == null)
            {
                var name = string.IsNullOrWhiteSpace(payload.Name)
                    ? email.Split('@')[0]
                    : payload.Name.Trim();
                var username = name[..Math.Min(name.Length, 50)];

                var tenant = new Tenant
                {
                    Id = Guid.NewGuid(),
                    Name = $"{username}'s Workspace",
                    Status = "Active",
                    CreatedAt = DateTime.UtcNow
                };
                await _tenantRepository.AddAsync(tenant, cancellationToken);

                user = new User
                {
                    Username = username,
                    Email = email,
                    TenantId = tenant.Id,
                    PasswordHash = _passwordHasher.HashPassword(
                        new User(),
                        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)))
                };
                user = await _userRepository.AddAsync(user, cancellationToken);
            }
            else
            {
                // Safe, idempotent tenant onboarding for legacy Google users without a TenantId
                await EnsureTenantAssignedAsync(user, cancellationToken);
            }

            return (user, GenerateJwtToken(user));
        }

        private async Task EnsureTenantAssignedAsync(User user, CancellationToken cancellationToken)
        {
            if (!user.TenantId.HasValue || user.TenantId.Value == Guid.Empty)
            {
                var tenant = new Tenant
                {
                    Id = Guid.NewGuid(),
                    Name = $"{user.Username.Trim()}'s Workspace",
                    Status = "Active",
                    CreatedAt = DateTime.UtcNow
                };
                await _tenantRepository.AddAsync(tenant, cancellationToken);

                user.TenantId = tenant.Id;
                await _userRepository.UpdateAsync(user, cancellationToken);
            }
        }

        private string GenerateJwtToken(User user)
        {
            var key = _configuration["Jwt:Key"]
                ?? throw new InvalidOperationException("JWT Key is not configured in application settings.");
            if (Encoding.UTF8.GetByteCount(key) < 32)
            {
                throw new InvalidOperationException("JWT Key must contain at least 32 bytes.");
            }
            var issuer = _configuration["Jwt:Issuer"];
            var audience = _configuration["Jwt:Audience"];
            var expiryMinutesStr = _configuration["Jwt:ExpiryMinutes"];

            if (!double.TryParse(expiryMinutesStr, out var expiryMinutes))
            {
                expiryMinutes = 60; // Default to 60 minutes if not specified or invalid
            }

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.AccountType.ToString()),
                new Claim("account_type", user.AccountType.ToString()),
                new Claim("sec_stamp", user.SecurityStamp),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            // Only emit tenant claim when user has an active assigned tenant
            if (user.TenantId.HasValue && user.TenantId.Value != Guid.Empty)
            {
                claims.Add(new Claim("tenant_id", user.TenantId.Value.ToString()));
            }

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
