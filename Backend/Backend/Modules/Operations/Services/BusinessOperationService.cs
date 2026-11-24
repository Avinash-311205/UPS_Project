using Backend.Modules.Operations.DTOs;
using Backend.Modules.Operations.Enums;
using Backend.Modules.Operations.Interfaces;
using Backend.Modules.Operations.Models;
using Backend.Modules.Systems.Enums;
using Backend.Modules.Systems.Interfaces;
using Backend.Modules.Tenants.Interfaces;

namespace Backend.Modules.Operations.Services
{
    public class BusinessOperationService : IBusinessOperationService
    {
        private readonly IBusinessOperationRepository _operationRepository;
        private readonly ILogisticsSystemRepository _systemRepository;
        private readonly ITenantContext _tenantContext;

        public BusinessOperationService(
            IBusinessOperationRepository operationRepository,
            ILogisticsSystemRepository systemRepository,
            ITenantContext tenantContext)
        {
            _operationRepository = operationRepository;
            _systemRepository = systemRepository;
            _tenantContext = tenantContext;
        }

        public async Task<IEnumerable<BusinessOperationResponseDTO>> GetAllAsync(
            bool includeInactive = false,
            CancellationToken cancellationToken = default)
        {
            if (!_tenantContext.HasTenant || !_tenantContext.TenantId.HasValue)
            {
                return Enumerable.Empty<BusinessOperationResponseDTO>();
            }

            var currentTenantId = _tenantContext.TenantId.Value;
            var operations = await _operationRepository.GetAllAsync(includeInactive, cancellationToken);
            return operations
                .Where(o => o.TenantId == currentTenantId)
                .Select(MapToResponse);
        }

        public async Task<BusinessOperationResponseDTO?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            if (!_tenantContext.HasTenant || !_tenantContext.TenantId.HasValue)
            {
                return null;
            }

            var operation = await _operationRepository.GetByIdAsync(id, cancellationToken);
            if (operation == null || operation.TenantId != _tenantContext.TenantId.Value)
            {
                return null;
            }

            return MapToResponse(operation);
        }

        public async Task<(bool Success, string? Error, bool IsConflict, BusinessOperationResponseDTO? Data)> CreateAsync(
            CreateBusinessOperationDTO dto,
            CancellationToken cancellationToken = default)
        {
            if (!_tenantContext.HasTenant || !_tenantContext.TenantId.HasValue)
            {
                return (false, "Tenant context is missing or unauthenticated.", false, null);
            }

            var currentTenantId = _tenantContext.TenantId.Value;

            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                return (false, "Operation name is required.", false, null);
            }

            var trimmedName = dto.Name.Trim();
            if (trimmedName.Length > 100)
            {
                return (false, "Operation name cannot exceed 100 characters.", false, null);
            }

            if (!Enum.IsDefined(typeof(OperationCriticality), dto.Criticality))
            {
                return (false, "Invalid Criticality value. Permitted values are Low (1), Medium (2), High (3), Critical (4).", false, null);
            }

            if (!Enum.IsDefined(typeof(AvailabilityRule), dto.AvailabilityRule))
            {
                return (false, "Invalid AvailabilityRule value. Permitted values are AllRequired (1), AnyAvailable (2).", false, null);
            }

            // Tenant-scoped uniqueness check
            var nameExists = await _operationRepository.ExistsByNameAsync(trimmedName, null, cancellationToken);
            if (nameExists)
            {
                return (false, $"A business operation with the name '{trimmedName}' already exists for this tenant.", true, null);
            }

            var operation = new BusinessOperation
            {
                Id = Guid.NewGuid(),
                TenantId = currentTenantId,
                Name = trimmedName,
                Description = dto.Description?.Trim(),
                LifecycleStatus = OperationLifecycleStatus.Active,
                Criticality = dto.Criticality,
                AvailabilityRule = dto.AvailabilityRule,
                CreatedAt = DateTime.UtcNow
            };

            var created = await _operationRepository.AddAsync(operation, cancellationToken);
            return (true, null, false, MapToResponse(created));
        }

        public async Task<(bool Success, string? Error, bool NotFound, bool IsConflict, BusinessOperationResponseDTO? Data)> UpdateAsync(
            Guid id,
            UpdateBusinessOperationDTO dto,
            CancellationToken cancellationToken = default)
        {
            if (!_tenantContext.HasTenant || !_tenantContext.TenantId.HasValue)
            {
                return (false, "Tenant context is missing or unauthenticated.", true, false, null);
            }

            var currentTenantId = _tenantContext.TenantId.Value;
            var operation = await _operationRepository.GetByIdAsync(id, cancellationToken);
            if (operation == null || operation.TenantId != currentTenantId)
            {
                return (false, $"Business operation with ID '{id}' was not found.", true, false, null);
            }

            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                return (false, "Operation name is required.", false, false, null);
            }

            var trimmedName = dto.Name.Trim();
            if (trimmedName.Length > 100)
            {
                return (false, "Operation name cannot exceed 100 characters.", false, false, null);
            }

            if (!Enum.IsDefined(typeof(OperationLifecycleStatus), dto.LifecycleStatus))
            {
                return (false, "Invalid LifecycleStatus value.", false, false, null);
            }

            if (!Enum.IsDefined(typeof(OperationCriticality), dto.Criticality))
            {
                return (false, "Invalid Criticality value. Permitted values are Low (1), Medium (2), High (3), Critical (4).", false, false, null);
            }

            if (!Enum.IsDefined(typeof(AvailabilityRule), dto.AvailabilityRule))
            {
                return (false, "Invalid AvailabilityRule value. Permitted values are AllRequired (1), AnyAvailable (2).", false, false, null);
            }

            // Tenant-scoped uniqueness check excluding current record
            var nameExists = await _operationRepository.ExistsByNameAsync(trimmedName, id, cancellationToken);
            if (nameExists)
            {
                return (false, $"A business operation with the name '{trimmedName}' already exists for this tenant.", false, true, null);
            }

            operation.Name = trimmedName;
            operation.Description = dto.Description?.Trim();
            operation.LifecycleStatus = dto.LifecycleStatus;
            operation.Criticality = dto.Criticality;
            operation.AvailabilityRule = dto.AvailabilityRule;
            operation.UpdatedAt = DateTime.UtcNow;

            var updated = await _operationRepository.UpdateAsync(operation, cancellationToken);
            return (true, null, false, false, MapToResponse(updated));
        }

        public async Task<(bool Success, string? Error, bool NotFound)> DeleteAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            if (!_tenantContext.HasTenant || !_tenantContext.TenantId.HasValue)
            {
                return (false, "Tenant context is missing or unauthenticated.", true);
            }

            var currentTenantId = _tenantContext.TenantId.Value;
            var operation = await _operationRepository.GetByIdAsync(id, cancellationToken);
            if (operation == null || operation.TenantId != currentTenantId)
            {
                return (false, $"Business operation with ID '{id}' was not found.", true);
            }

            // Soft deactivation: preserves record for impact analysis, incident management and audit tracking
            operation.LifecycleStatus = OperationLifecycleStatus.Inactive;
            operation.UpdatedAt = DateTime.UtcNow;

            await _operationRepository.UpdateAsync(operation, cancellationToken);
            return (true, null, false);
        }

        public async Task<(bool Success, string? Error, bool NotFound, bool IsConflict, SystemBusinessOperationResponseDTO? Data)> AssignSystemAsync(
            Guid operationId,
            Guid systemId,
            CancellationToken cancellationToken = default)
        {
            if (!_tenantContext.HasTenant || !_tenantContext.TenantId.HasValue)
            {
                return (false, "Tenant context is missing or unauthenticated.", true, false, null);
            }

            var currentTenantId = _tenantContext.TenantId.Value;

            // 1. Verify BusinessOperation exists and belongs to tenant
            var operation = await _operationRepository.GetByIdAsync(operationId, cancellationToken);
            if (operation == null || operation.TenantId != currentTenantId)
            {
                return (false, $"Business operation with ID '{operationId}' was not found.", true, false, null);
            }

            if (operation.LifecycleStatus != OperationLifecycleStatus.Active)
            {
                return (false, "Cannot assign systems to an inactive business operation.", false, false, null);
            }

            // 2. Verify LogisticsSystem exists and belongs to tenant
            var system = await _systemRepository.GetByIdAsync(systemId, cancellationToken);
            if (system == null || system.TenantId != currentTenantId)
            {
                return (false, $"Logistics system with ID '{systemId}' was not found.", true, false, null);
            }

            // Inactive systems cannot be assigned new operations
            if (system.LifecycleStatus != SystemLifecycleStatus.Active)
            {
                return (false, $"Cannot assign business operations to an inactive logistics system '{system.Name}'.", false, false, null);
            }

            // 3. Prevent duplicate system-operation relationships
            var alreadyAssigned = await _operationRepository.AssignmentExistsAsync(operationId, systemId, cancellationToken);
            if (alreadyAssigned)
            {
                return (false, $"Logistics system '{system.Name}' is already assigned to business operation '{operation.Name}'.", false, true, null);
            }

            // 4. Create assignment
            var assignment = new SystemBusinessOperation
            {
                Id = Guid.NewGuid(),
                TenantId = currentTenantId,
                BusinessOperationId = operationId,
                LogisticsSystemId = systemId,
                CreatedAt = DateTime.UtcNow
            };

            await _operationRepository.AddAssignmentAsync(assignment, cancellationToken);

            var response = new SystemBusinessOperationResponseDTO
            {
                Id = assignment.Id,
                TenantId = assignment.TenantId,
                BusinessOperationId = operation.Id,
                BusinessOperationName = operation.Name,
                LogisticsSystemId = system.Id,
                LogisticsSystemName = system.Name,
                CreatedAt = assignment.CreatedAt
            };

            return (true, null, false, false, response);
        }

        public async Task<(bool Success, string? Error, bool NotFound)> RemoveSystemAsync(
            Guid operationId,
            Guid systemId,
            CancellationToken cancellationToken = default)
        {
            if (!_tenantContext.HasTenant || !_tenantContext.TenantId.HasValue)
            {
                return (false, "Tenant context is missing or unauthenticated.", true);
            }

            var currentTenantId = _tenantContext.TenantId.Value;

            var assignment = await _operationRepository.GetAssignmentAsync(operationId, systemId, cancellationToken);
            if (assignment == null || assignment.TenantId != currentTenantId)
            {
                return (false, $"Assignment between business operation '{operationId}' and system '{systemId}' was not found.", true);
            }

            // Deletes ONLY the relationship link; does NOT delete the system or the operation
            await _operationRepository.RemoveAssignmentAsync(assignment, cancellationToken);
            return (true, null, false);
        }

        public async Task<(bool Success, string? Error, bool NotFound, IEnumerable<SystemBusinessOperationResponseDTO>? Data)> GetSystemsForOperationAsync(
            Guid operationId,
            CancellationToken cancellationToken = default)
        {
            if (!_tenantContext.HasTenant || !_tenantContext.TenantId.HasValue)
            {
                return (false, "Tenant context is missing or unauthenticated.", true, null);
            }

            var currentTenantId = _tenantContext.TenantId.Value;
            var operation = await _operationRepository.GetByIdAsync(operationId, cancellationToken);
            if (operation == null || operation.TenantId != currentTenantId)
            {
                return (false, $"Business operation with ID '{operationId}' was not found.", true, null);
            }

            var assignments = await _operationRepository.GetSystemsByOperationIdAsync(operationId, cancellationToken);
            var results = assignments
                .Where(a => a.TenantId == currentTenantId)
                .Select(a => new SystemBusinessOperationResponseDTO
                {
                    Id = a.Id,
                    TenantId = a.TenantId,
                    BusinessOperationId = operation.Id,
                    BusinessOperationName = operation.Name,
                    LogisticsSystemId = a.LogisticsSystemId,
                    LogisticsSystemName = a.LogisticsSystem?.Name ?? string.Empty,
                    CreatedAt = a.CreatedAt
                });

            return (true, null, false, results);
        }

        public async Task<(bool Success, string? Error, bool NotFound, IEnumerable<SystemBusinessOperationResponseDTO>? Data)> GetOperationsForSystemAsync(
            Guid systemId,
            CancellationToken cancellationToken = default)
        {
            if (!_tenantContext.HasTenant || !_tenantContext.TenantId.HasValue)
            {
                return (false, "Tenant context is missing or unauthenticated.", true, null);
            }

            var currentTenantId = _tenantContext.TenantId.Value;
            var system = await _systemRepository.GetByIdAsync(systemId, cancellationToken);
            if (system == null || system.TenantId != currentTenantId)
            {
                return (false, $"Logistics system with ID '{systemId}' was not found.", true, null);
            }

            var assignments = await _operationRepository.GetOperationsBySystemIdAsync(systemId, cancellationToken);
            var results = assignments
                .Where(a => a.TenantId == currentTenantId)
                .Select(a => new SystemBusinessOperationResponseDTO
                {
                    Id = a.Id,
                    TenantId = a.TenantId,
                    BusinessOperationId = a.BusinessOperationId,
                    BusinessOperationName = a.BusinessOperation?.Name ?? string.Empty,
                    LogisticsSystemId = system.Id,
                    LogisticsSystemName = system.Name,
                    CreatedAt = a.CreatedAt
                });

            return (true, null, false, results);
        }

        private static BusinessOperationResponseDTO MapToResponse(BusinessOperation operation)
        {
            return new BusinessOperationResponseDTO
            {
                Id = operation.Id,
                TenantId = operation.TenantId,
                Name = operation.Name,
                Description = operation.Description,
                LifecycleStatus = operation.LifecycleStatus,
                Criticality = operation.Criticality,
                AvailabilityRule = operation.AvailabilityRule,
                CreatedAt = operation.CreatedAt,
                UpdatedAt = operation.UpdatedAt,
                SupportingSystemsCount = operation.SupportingSystems?.Count ?? 0
            };
        }
    }
}
