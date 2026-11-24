using Backend.Modules.Dependencies.DTOs;
using Backend.Modules.Dependencies.Enums;
using Backend.Modules.Dependencies.Interfaces;
using Backend.Modules.Dependencies.Models;
using Backend.Modules.Systems.DTOs;
using Backend.Modules.Systems.Enums;
using Backend.Modules.Systems.Interfaces;
using Backend.Modules.Systems.Models;
using Backend.Modules.Tenants.Interfaces;

namespace Backend.Modules.Dependencies.Services
{
    public class SystemDependencyService : ISystemDependencyService
    {
        private readonly ISystemDependencyRepository _dependencyRepository;
        private readonly ILogisticsSystemRepository _systemRepository;
        private readonly ITenantContext _tenantContext;

        public SystemDependencyService(
            ISystemDependencyRepository dependencyRepository,
            ILogisticsSystemRepository systemRepository,
            ITenantContext tenantContext)
        {
            _dependencyRepository = dependencyRepository;
            _systemRepository = systemRepository;
            _tenantContext = tenantContext;
        }

        public async Task<IEnumerable<SystemDependencyResponseDTO>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            if (!_tenantContext.HasTenant || !_tenantContext.TenantId.HasValue)
            {
                return Enumerable.Empty<SystemDependencyResponseDTO>();
            }

            var currentTenantId = _tenantContext.TenantId.Value;
            var dependencies = await _dependencyRepository.GetAllAsync(cancellationToken);
            return dependencies
                .Where(d => d.TenantId == currentTenantId)
                .Select(MapToResponse);
        }

        public async Task<SystemDependencyResponseDTO?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            if (!_tenantContext.HasTenant || !_tenantContext.TenantId.HasValue)
            {
                return null;
            }

            var dependency = await _dependencyRepository.GetByIdAsync(id, cancellationToken);
            if (dependency == null || dependency.TenantId != _tenantContext.TenantId.Value)
            {
                return null;
            }

            return MapToResponse(dependency);
        }

        public async Task<(bool Success, string? Error, SystemDependencyResponseDTO? Data)> CreateAsync(
            CreateSystemDependencyDTO dto,
            CancellationToken cancellationToken = default)
        {
            if (!_tenantContext.HasTenant || !_tenantContext.TenantId.HasValue)
            {
                return (false, "An authenticated tenant context is required to register a dependency.", null);
            }

            var currentTenantId = _tenantContext.TenantId.Value;

            // Rule 5: A system cannot depend on itself
            if (dto.DependentSystemId == dto.DependencySystemId)
            {
                return (false, "A system cannot depend on itself.", null);
            }

            // Rule 7: DependencyType must be Mandatory or Optional
            if (!Enum.IsDefined(typeof(DependencyType), dto.DependencyType))
            {
                return (false, $"Invalid DependencyType value '{dto.DependencyType}'.", null);
            }

            // Rule 1: Dependent system must exist
            var dependentSystem = await _systemRepository.GetByIdAsync(dto.DependentSystemId, cancellationToken);
            if (dependentSystem == null)
            {
                return (false, $"Dependent system with ID '{dto.DependentSystemId}' does not exist.", null);
            }

            // Rule 2: Dependency system must exist
            var dependencySystem = await _systemRepository.GetByIdAsync(dto.DependencySystemId, cancellationToken);
            if (dependencySystem == null)
            {
                return (false, $"Dependency system with ID '{dto.DependencySystemId}' does not exist.", null);
            }

            // Rule 3: Both systems must belong to the authenticated user's tenant
            if (dependentSystem.TenantId != currentTenantId || dependencySystem.TenantId != currentTenantId)
            {
                return (false, "Both systems must belong to the authenticated user's tenant. Cross-tenant dependencies are strictly prohibited.", null);
            }

            // Rule 4: Both systems must be Active when creating a dependency
            if (dependentSystem.LifecycleStatus != SystemLifecycleStatus.Active)
            {
                return (false, $"Dependent system '{dependentSystem.Name}' is inactive. Dependencies cannot be created with inactive systems.", null);
            }

            if (dependencySystem.LifecycleStatus != SystemLifecycleStatus.Active)
            {
                return (false, $"Dependency system '{dependencySystem.Name}' is inactive. Dependencies cannot be created with inactive systems.", null);
            }

            // Rule 6: Duplicate relationships are prohibited
            var exists = await _dependencyRepository.ExistsAsync(dto.DependentSystemId, dto.DependencySystemId, cancellationToken);
            if (exists)
            {
                return (false, "A dependency relationship between these systems already exists.", null);
            }

            var dependency = new SystemDependency
            {
                Id = Guid.NewGuid(),
                TenantId = currentTenantId,
                DependentSystemId = dto.DependentSystemId,
                DependencySystemId = dto.DependencySystemId,
                DependencyType = dto.DependencyType,
                CreatedAt = DateTime.UtcNow
            };

            var created = await _dependencyRepository.AddAsync(dependency, cancellationToken);
            
            // Populate navigation references for response mapping
            created.DependentSystem = dependentSystem;
            created.DependencySystem = dependencySystem;

            return (true, null, MapToResponse(created));
        }

        public async Task<(bool Success, string? Error, bool NotFound, SystemDependencyResponseDTO? Data)> UpdateAsync(
            Guid id,
            UpdateSystemDependencyDTO dto,
            CancellationToken cancellationToken = default)
        {
            if (!_tenantContext.HasTenant || !_tenantContext.TenantId.HasValue)
            {
                return (false, "An authenticated tenant context is required.", true, null);
            }

            var existing = await _dependencyRepository.GetByIdAsync(id, cancellationToken);
            if (existing == null || existing.TenantId != _tenantContext.TenantId.Value)
            {
                return (false, $"System dependency with ID '{id}' was not found.", true, null);
            }

            // Rule 7: DependencyType must be Mandatory or Optional
            if (!Enum.IsDefined(typeof(DependencyType), dto.DependencyType))
            {
                return (false, $"Invalid DependencyType value '{dto.DependencyType}'.", false, null);
            }

            // Rule 8: DependencyType can be updated on an existing relationship
            existing.DependencyType = dto.DependencyType;
            existing.UpdatedAt = DateTime.UtcNow;

            var updated = await _dependencyRepository.UpdateAsync(existing, cancellationToken);
            return (true, null, false, MapToResponse(updated));
        }

        public async Task<(bool Success, string? Error, bool NotFound)> DeleteAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            if (!_tenantContext.HasTenant || !_tenantContext.TenantId.HasValue)
            {
                return (false, "An authenticated tenant context is required.", true);
            }

            var existing = await _dependencyRepository.GetByIdAsync(id, cancellationToken);
            if (existing == null || existing.TenantId != _tenantContext.TenantId.Value)
            {
                return (false, $"System dependency with ID '{id}' was not found.", true);
            }

            // Deletion: Remove only the selected dependency relationship, never deleting either LogisticsSystem
            await _dependencyRepository.DeleteAsync(existing, cancellationToken);
            return (true, null, false);
        }

        private static SystemDependencyResponseDTO MapToResponse(SystemDependency dependency)
        {
            return new SystemDependencyResponseDTO
            {
                Id = dependency.Id,
                TenantId = dependency.TenantId,
                DependentSystemId = dependency.DependentSystemId,
                DependentSystem = dependency.DependentSystem != null ? MapSystemSummary(dependency.DependentSystem) : null,
                DependencySystemId = dependency.DependencySystemId,
                DependencySystem = dependency.DependencySystem != null ? MapSystemSummary(dependency.DependencySystem) : null,
                DependencyType = dependency.DependencyType,
                CreatedAt = dependency.CreatedAt,
                UpdatedAt = dependency.UpdatedAt
            };
        }

        private static LogisticsSystemResponseDTO MapSystemSummary(LogisticsSystem system)
        {
            return new LogisticsSystemResponseDTO
            {
                Id = system.Id,
                TenantId = system.TenantId,
                Name = system.Name,
                SystemType = system.SystemType,
                Description = system.Description,
                BusinessPurpose = system.BusinessPurpose,
                Criticality = system.Criticality,
                Environment = system.Environment,
                LifecycleStatus = system.LifecycleStatus,
                CreatedAt = system.CreatedAt,
                UpdatedAt = system.UpdatedAt
            };
        }
    }
}
