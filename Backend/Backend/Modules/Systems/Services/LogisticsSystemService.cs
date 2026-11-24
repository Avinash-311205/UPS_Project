using Backend.Modules.Systems.DTOs;
using Backend.Modules.Systems.Enums;
using Backend.Modules.Systems.Interfaces;
using Backend.Modules.Systems.Models;
using Backend.Modules.Tenants.Interfaces;

namespace Backend.Modules.Systems.Services
{
    public class LogisticsSystemService : ILogisticsSystemService
    {
        private readonly ILogisticsSystemRepository _repository;
        private readonly ITenantContext _tenantContext;

        public LogisticsSystemService(
            ILogisticsSystemRepository repository,
            ITenantContext tenantContext)
        {
            _repository = repository;
            _tenantContext = tenantContext;
        }

        public async Task<IEnumerable<LogisticsSystemResponseDTO>> GetAllAsync(
            bool includeInactive = false,
            CancellationToken cancellationToken = default)
        {
            if (!_tenantContext.HasTenant || !_tenantContext.TenantId.HasValue)
            {
                return Enumerable.Empty<LogisticsSystemResponseDTO>();
            }

            var systems = await _repository.GetAllAsync(cancellationToken);
            if (!includeInactive)
            {
                systems = systems.Where(s => s.LifecycleStatus == SystemLifecycleStatus.Active);
            }

            return systems.Select(MapToResponse);
        }

        public async Task<LogisticsSystemResponseDTO?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            if (!_tenantContext.HasTenant || !_tenantContext.TenantId.HasValue)
            {
                return null;
            }

            var system = await _repository.GetByIdAsync(id, cancellationToken);
            if (system == null || system.TenantId != _tenantContext.TenantId.Value)
            {
                return null;
            }

            return MapToResponse(system);
        }

        public async Task<(bool Success, string? Error, LogisticsSystemResponseDTO? Data)> CreateAsync(
            CreateLogisticsSystemDTO dto,
            CancellationToken cancellationToken = default)
        {
            if (!_tenantContext.HasTenant || !_tenantContext.TenantId.HasValue)
            {
                return (false, "An authenticated tenant context is required to register a logistics system.", null);
            }

            var validationError = ValidateDto(dto.Name, dto.Description, dto.BusinessPurpose, dto.SystemType, dto.Criticality, dto.Environment, dto.LifecycleStatus);
            if (validationError != null)
            {
                return (false, validationError, null);
            }

            var system = new LogisticsSystem
            {
                Id = Guid.NewGuid(),
                TenantId = _tenantContext.TenantId.Value,
                Name = dto.Name.Trim(),
                SystemType = dto.SystemType,
                Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
                BusinessPurpose = string.IsNullOrWhiteSpace(dto.BusinessPurpose) ? null : dto.BusinessPurpose.Trim(),
                Criticality = dto.Criticality,
                Environment = dto.Environment,
                LifecycleStatus = dto.LifecycleStatus,
                CreatedAt = DateTime.UtcNow
            };

            var created = await _repository.AddAsync(system, cancellationToken);
            return (true, null, MapToResponse(created));
        }

        public async Task<(bool Success, string? Error, bool NotFound, LogisticsSystemResponseDTO? Data)> UpdateAsync(
            Guid id,
            UpdateLogisticsSystemDTO dto,
            CancellationToken cancellationToken = default)
        {
            if (!_tenantContext.HasTenant || !_tenantContext.TenantId.HasValue)
            {
                return (false, "An authenticated tenant context is required.", true, null);
            }

            var existing = await _repository.GetByIdAsync(id, cancellationToken);
            if (existing == null || existing.TenantId != _tenantContext.TenantId.Value)
            {
                return (false, $"Logistics system with ID '{id}' was not found.", true, null);
            }

            var validationError = ValidateDto(dto.Name, dto.Description, dto.BusinessPurpose, dto.SystemType, dto.Criticality, dto.Environment, dto.LifecycleStatus);
            if (validationError != null)
            {
                return (false, validationError, false, null);
            }

            existing.Name = dto.Name.Trim();
            existing.SystemType = dto.SystemType;
            existing.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();
            existing.BusinessPurpose = string.IsNullOrWhiteSpace(dto.BusinessPurpose) ? null : dto.BusinessPurpose.Trim();
            existing.Criticality = dto.Criticality;
            existing.Environment = dto.Environment;
            existing.LifecycleStatus = dto.LifecycleStatus;
            existing.UpdatedAt = DateTime.UtcNow;

            var updated = await _repository.UpdateAsync(existing, cancellationToken);
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

            var existing = await _repository.GetByIdAsync(id, cancellationToken);
            if (existing == null || existing.TenantId != _tenantContext.TenantId.Value)
            {
                return (false, $"Logistics system with ID '{id}' was not found.", true);
            }

            // Soft deactivation: Preserves system identity and history for future
            // Dependency Management, Incident Management, and immutable audit logs.
            existing.LifecycleStatus = SystemLifecycleStatus.Inactive;
            existing.UpdatedAt = DateTime.UtcNow;

            await _repository.UpdateAsync(existing, cancellationToken);
            return (true, null, false);
        }

        private static string? ValidateDto(
            string name,
            string? description,
            string? businessPurpose,
            SystemType systemType,
            SystemCriticality criticality,
            SystemEnvironment environment,
            SystemLifecycleStatus lifecycleStatus)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return "System name is required.";
            }

            if (name.Trim().Length > 100)
            {
                return "System name cannot exceed 100 characters.";
            }

            if (description?.Length > 500)
            {
                return "Description cannot exceed 500 characters.";
            }

            if (businessPurpose?.Length > 500)
            {
                return "Business purpose cannot exceed 500 characters.";
            }

            if (!Enum.IsDefined(typeof(SystemType), systemType))
            {
                return $"Invalid SystemType value '{systemType}'.";
            }

            if (!Enum.IsDefined(typeof(SystemCriticality), criticality))
            {
                return $"Invalid Criticality value '{criticality}'.";
            }

            if (!Enum.IsDefined(typeof(SystemEnvironment), environment))
            {
                return $"Invalid Environment value '{environment}'.";
            }

            if (!Enum.IsDefined(typeof(SystemLifecycleStatus), lifecycleStatus))
            {
                return $"Invalid LifecycleStatus value '{lifecycleStatus}'.";
            }

            return null;
        }

        private static LogisticsSystemResponseDTO MapToResponse(LogisticsSystem system)
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
