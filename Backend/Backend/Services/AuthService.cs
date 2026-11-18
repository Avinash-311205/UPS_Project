using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Security.Claims;
using System.Text;
using Backend.DTO;
using Backend.Interface;
using Backend.Model;
using Google.Apis.Auth;
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

        public async Task<(User User, string Token)?> LoginAsync(LoginDTO loginDto, CancellationToken cancellationToken = default)
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

                user = new User
                {
                    Username = username,
                    Email = email,
                    PasswordHash = _passwordHasher.HashPassword(
                        new User(),
                        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)))
                };
                user = await _userRepository.AddAsync(user, cancellationToken);
            }

            return (user, GenerateJwtToken(user));
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
