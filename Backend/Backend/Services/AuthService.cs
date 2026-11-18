using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Backend.DTO;
using Backend.Interface;
using Backend.Model;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace Backend.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly IConfiguration _configuration;

        public AuthService(
            IUserRepository userRepository,
            IPasswordHasher<User> passwordHasher,
            IConfiguration configuration)
        {
            _userRepository = userRepository;
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

            // Create user entity
            var user = new User
            {
                Username = registerDto.Username,
                Email = registerDto.Email
            };

            // Hash password and store only the hash
            user.PasswordHash = _passwordHasher.HashPassword(user, registerDto.Password);

            // Persist user via repository
            var createdUser = await _userRepository.AddAsync(user, cancellationToken);

            // Ensure PasswordHash is not exposed in returned entity
            createdUser.PasswordHash = string.Empty;

            return createdUser;
        }

        public async Task<string?> LoginAsync(LoginDTO loginDto, CancellationToken cancellationToken = default)
        {
            // Retrieve user by email
            var user = await _userRepository.GetByEmailAsync(loginDto.Email, cancellationToken);
            if (user == null)
            {
                return null;
            }

            // Verify password against stored hash
            var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, loginDto.Password);
            if (verificationResult == PasswordVerificationResult.Failed)
            {
                return null;
            }

            // Generate and return signed JWT token
            return GenerateJwtToken(user);
        }

        private string GenerateJwtToken(User user)
        {
            var key = _configuration["Jwt:Key"]
                ?? throw new InvalidOperationException("JWT Key is not configured in application settings.");
            var issuer = _configuration["Jwt:Issuer"];
            var audience = _configuration["Jwt:Audience"];
            var expiryMinutesStr = _configuration["Jwt:ExpiryMinutes"];

            if (!double.TryParse(expiryMinutesStr, out var expiryMinutes))
            {
                expiryMinutes = 60; // Default to 60 minutes if not specified or invalid
            }

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

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
