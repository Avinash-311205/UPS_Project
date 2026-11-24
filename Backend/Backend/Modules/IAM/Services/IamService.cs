using Backend.Modules.IAM.DTOs;
using Backend.Modules.IAM.Interfaces;
using Backend.Modules.Identity.Enums;
using Backend.Modules.Identity.Interfaces;
using Backend.Modules.Identity.Models;
using Backend.Modules.Tenants.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace Backend.Modules.IAM.Services
{
    public class IamService : IIamService
    {
        private readonly IUserRepository _userRepository;
        private readonly ITenantContext _tenantContext;
        private readonly IPasswordHasher<User> _passwordHasher;

        public IamService(
            IUserRepository userRepository,
            ITenantContext tenantContext,
            IPasswordHasher<User> passwordHasher)
        {
            _userRepository = userRepository;
            _tenantContext = tenantContext;
            _passwordHasher = passwordHasher;
        }

        public async Task<IEnumerable<IamUserResponseDTO>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            if (!_tenantContext.HasTenant)
            {
                throw new UnauthorizedAccessException("Tenant context is required.");
            }

            var currentTenantId = _tenantContext.TenantId!.Value;
            var users = await _userRepository.GetIamUsersByTenantAsync(currentTenantId, cancellationToken);

            return users.Select(u => new IamUserResponseDTO
            {
                Id = u.Id,
                Username = u.Username,
                Email = u.Email,
                TenantId = u.TenantId,
                AccountType = u.AccountType,
                Status = u.Status,
                CreatedAt = u.CreatedAt,
                UpdatedAt = u.UpdatedAt
            });
        }

        public async Task<IamUserResponseDTO> CreateAsync(CreateIamUserDTO dto, CancellationToken cancellationToken = default)
        {
            if (!_tenantContext.HasTenant)
            {
                throw new UnauthorizedAccessException("Tenant context is required.");
            }

            // Enforce global email uniqueness across all tenants
            var exists = await _userRepository.EmailExistsAsync(dto.Email, cancellationToken);
            if (exists)
            {
                throw new InvalidOperationException("A user with this email already exists.");
            }

            var currentTenantId = _tenantContext.TenantId!.Value;

            var newUser = new User
            {
                Username = dto.Username.Trim(),
                Email = dto.Email.Trim(),
                TenantId = currentTenantId,
                AccountType = AccountType.IAM,
                Status = UserStatus.Active,
                SecurityStamp = Guid.NewGuid().ToString(),
                CreatedAt = DateTime.UtcNow
            };

            newUser.PasswordHash = _passwordHasher.HashPassword(newUser, dto.Password);

            await _userRepository.AddAsync(newUser, cancellationToken);

            return new IamUserResponseDTO
            {
                Id = newUser.Id,
                Username = newUser.Username,
                Email = newUser.Email,
                TenantId = newUser.TenantId,
                AccountType = newUser.AccountType,
                Status = newUser.Status,
                CreatedAt = newUser.CreatedAt,
                UpdatedAt = newUser.UpdatedAt
            };
        }

        public async Task<IamUserResponseDTO?> UpdateStatusAsync(
            int id,
            UpdateIamUserStatusDTO dto,
            int callerUserId,
            CancellationToken cancellationToken = default)
        {
            if (!_tenantContext.HasTenant)
            {
                throw new UnauthorizedAccessException("Tenant context is required.");
            }

            var currentTenantId = _tenantContext.TenantId!.Value;
            var user = await _userRepository.GetByIdAndTenantAsync(id, currentTenantId, cancellationToken);

            if (user == null)
            {
                return null;
            }

            if (user.Id == callerUserId)
            {
                throw new InvalidOperationException("Users cannot modify their own active status.");
            }

            if (user.AccountType == AccountType.Root)
            {
                throw new InvalidOperationException("Root account status cannot be modified via IAM management.");
            }

            user.Status = dto.Status;

            // Invalidate existing sessions immediately when deactivating an account
            if (dto.Status == UserStatus.Inactive)
            {
                user.SecurityStamp = Guid.NewGuid().ToString();
            }

            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user, cancellationToken);

            return new IamUserResponseDTO
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                TenantId = user.TenantId,
                AccountType = user.AccountType,
                Status = user.Status,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            };
        }

        public async Task<bool> ResetPasswordAsync(
            int id,
            ResetIamUserPasswordDTO dto,
            CancellationToken cancellationToken = default)
        {
            if (!_tenantContext.HasTenant)
            {
                throw new UnauthorizedAccessException("Tenant context is required.");
            }

            var currentTenantId = _tenantContext.TenantId!.Value;
            var user = await _userRepository.GetByIdAndTenantAsync(id, currentTenantId, cancellationToken);

            if (user == null)
            {
                return false;
            }

            if (user.AccountType == AccountType.Root)
            {
                throw new InvalidOperationException("Password reset for Root account is not permitted via IAM endpoints.");
            }

            user.PasswordHash = _passwordHasher.HashPassword(user, dto.NewPassword);
            user.SecurityStamp = Guid.NewGuid().ToString(); // Invalidate existing sessions
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user, cancellationToken);

            return true;
        }
    }
}
