using Backend.Modules.ImpactAnalysis.DTOs;
using Backend.Modules.ImpactAnalysis.Interfaces;
using Backend.Modules.Incidents.DTOs;
using Backend.Modules.Incidents.Enums;
using Backend.Modules.Incidents.Interfaces;
using Backend.Modules.Incidents.Models;
using Backend.Modules.Systems.Enums;
using Backend.Modules.Systems.Interfaces;
using Backend.Modules.Tenants.Interfaces;

namespace Backend.Modules.Incidents.Services
{
    public class IncidentService : IIncidentService
    {
        private readonly IIncidentRepository _incidentRepository;
        private readonly ILogisticsSystemRepository _systemRepository;
        private readonly IImpactAnalysisService _impactAnalysisService;
        private readonly ITenantContext _tenantContext;

        public IncidentService(
            IIncidentRepository incidentRepository,
            ILogisticsSystemRepository systemRepository,
            IImpactAnalysisService impactAnalysisService,
            ITenantContext tenantContext)
        {
            _incidentRepository = incidentRepository;
            _systemRepository = systemRepository;
            _impactAnalysisService = impactAnalysisService;
            _tenantContext = tenantContext;
        }

        public async Task<IEnumerable<IncidentResponseDTO>> GetAllAsync(
            IncidentStatus? status = null,
            IncidentSeverity? severity = null,
            IncidentType? type = null,
            Guid? failedSystemId = null,
            CancellationToken cancellationToken = default)
        {
            if (!_tenantContext.HasTenant || !_tenantContext.TenantId.HasValue)
            {
                return Enumerable.Empty<IncidentResponseDTO>();
            }

            var currentTenantId = _tenantContext.TenantId.Value;
            var incidents = await _incidentRepository.GetAllAsync(status, severity, type, failedSystemId, cancellationToken);
            return incidents
                .Where(i => i.TenantId == currentTenantId)
                .Select(MapToResponse);
        }

        public async Task<IncidentResponseDTO?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            if (!_tenantContext.HasTenant || !_tenantContext.TenantId.HasValue)
            {
                return null;
            }

            var incident = await _incidentRepository.GetByIdAsync(id, cancellationToken);
            if (incident == null || incident.TenantId != _tenantContext.TenantId.Value)
            {
                return null;
            }

            return MapToResponse(incident);
        }

        public async Task<(bool Success, string? Error, bool NotFound, IncidentResponseDTO? Data)> CreateAsync(
            CreateIncidentDTO dto,
            CancellationToken cancellationToken = default)
        {
            if (!_tenantContext.HasTenant || !_tenantContext.TenantId.HasValue)
            {
                return (false, "An authenticated tenant context is required to create an incident.", false, null);
            }

            var currentTenantId = _tenantContext.TenantId.Value;

            if (string.IsNullOrWhiteSpace(dto.Title))
            {
                return (false, "Incident title is required.", false, null);
            }

            var trimmedTitle = dto.Title.Trim();
            if (trimmedTitle.Length > 200)
            {
                return (false, "Incident title cannot exceed 200 characters.", false, null);
            }

            if (!Enum.IsDefined(typeof(IncidentType), dto.IncidentType))
            {
                return (false, "Invalid IncidentType value.", false, null);
            }

            if (!Enum.IsDefined(typeof(IncidentSeverity), dto.Severity))
            {
                return (false, "Invalid Severity value.", false, null);
            }

            // Verify failed system exists and belongs to the authenticated tenant
            var system = await _systemRepository.GetByIdAsync(dto.FailedSystemId, cancellationToken);
            if (system == null || system.TenantId != currentTenantId)
            {
                return (false, $"Logistics system with ID '{dto.FailedSystemId}' was not found.", true, null);
            }

            // Inactive systems cannot have new incidents created
            if (system.LifecycleStatus != SystemLifecycleStatus.Active)
            {
                return (false, $"Cannot report incident for inactive system '{system.Name}'.", false, null);
            }

            var incident = new Incident
            {
                Id = Guid.NewGuid(),
                TenantId = currentTenantId,
                FailedSystemId = system.Id,
                FailedSystem = system,
                Title = trimmedTitle,
                Description = dto.Description?.Trim(),
                IncidentType = dto.IncidentType,
                Severity = dto.Severity,
                Status = IncidentStatus.Open,
                DetectedAt = dto.DetectedAt ?? DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            var created = await _incidentRepository.AddAsync(incident, cancellationToken);
            return (true, null, false, MapToResponse(created));
        }

        public async Task<(bool Success, string? Error, bool NotFound, IncidentResponseDTO? Data)> UpdateAsync(
            Guid id,
            UpdateIncidentDTO dto,
            CancellationToken cancellationToken = default)
        {
            if (!_tenantContext.HasTenant || !_tenantContext.TenantId.HasValue)
            {
                return (false, "An authenticated tenant context is required to update an incident.", true, null);
            }

            var currentTenantId = _tenantContext.TenantId.Value;
            var incident = await _incidentRepository.GetByIdAsync(id, cancellationToken);
            if (incident == null || incident.TenantId != currentTenantId)
            {
                return (false, $"Incident with ID '{id}' was not found.", true, null);
            }

            if (incident.Status == IncidentStatus.Closed)
            {
                return (false, "Cannot modify an incident that is already Closed.", false, null);
            }

            if (string.IsNullOrWhiteSpace(dto.Title))
            {
                return (false, "Incident title is required.", false, null);
            }

            var trimmedTitle = dto.Title.Trim();
            if (trimmedTitle.Length > 200)
            {
                return (false, "Incident title cannot exceed 200 characters.", false, null);
            }

            if (!Enum.IsDefined(typeof(IncidentType), dto.IncidentType))
            {
                return (false, "Invalid IncidentType value.", false, null);
            }

            if (!Enum.IsDefined(typeof(IncidentSeverity), dto.Severity))
            {
                return (false, "Invalid Severity value.", false, null);
            }

            incident.Title = trimmedTitle;
            incident.Description = dto.Description?.Trim();
            incident.IncidentType = dto.IncidentType;
            incident.Severity = dto.Severity;
            incident.UpdatedAt = DateTime.UtcNow;

            var updated = await _incidentRepository.UpdateAsync(incident, cancellationToken);
            return (true, null, false, MapToResponse(updated));
        }

        public async Task<(bool Success, string? Error, bool NotFound, IncidentResponseDTO? Data)> UpdateStatusAsync(
            Guid id,
            UpdateIncidentStatusDTO dto,
            CancellationToken cancellationToken = default)
        {
            if (!_tenantContext.HasTenant || !_tenantContext.TenantId.HasValue)
            {
                return (false, "An authenticated tenant context is required to update incident status.", true, null);
            }

            var currentTenantId = _tenantContext.TenantId.Value;
            var incident = await _incidentRepository.GetByIdAsync(id, cancellationToken);
            if (incident == null || incident.TenantId != currentTenantId)
            {
                return (false, $"Incident with ID '{id}' was not found.", true, null);
            }

            if (!Enum.IsDefined(typeof(IncidentStatus), dto.Status))
            {
                return (false, "Invalid Status value.", false, null);
            }

            // Terminal Closed state rule: once closed, status cannot be transitioned
            if (incident.Status == IncidentStatus.Closed)
            {
                return (false, "Incident is Closed and cannot transition to any other status.", false, null);
            }

            // Check transition validity
            var isValidTransition = IsValidTransition(incident.Status, dto.Status);
            if (!isValidTransition)
            {
                return (false, $"Invalid status transition from '{incident.Status}' to '{dto.Status}'.", false, null);
            }

            // Status transition actions
            if (dto.Status == IncidentStatus.Resolved)
            {
                incident.ResolvedAt ??= DateTime.UtcNow;
            }
            else if (dto.Status == IncidentStatus.Closed)
            {
                incident.ResolvedAt ??= DateTime.UtcNow;
            }
            else if (incident.Status == IncidentStatus.Resolved && (dto.Status == IncidentStatus.Investigating || dto.Status == IncidentStatus.Mitigating || dto.Status == IncidentStatus.Open))
            {
                // Reopened
                incident.ResolvedAt = null;
            }

            incident.Status = dto.Status;
            incident.UpdatedAt = DateTime.UtcNow;

            if (!string.IsNullOrWhiteSpace(dto.ResolutionNotes))
            {
                var note = $"[{DateTime.UtcNow:u}] Status changed to {dto.Status}: {dto.ResolutionNotes.Trim()}";
                incident.Description = string.IsNullOrWhiteSpace(incident.Description)
                    ? note
                    : $"{incident.Description}\n{note}";
            }

            var updated = await _incidentRepository.UpdateAsync(incident, cancellationToken);
            return (true, null, false, MapToResponse(updated));
        }

        public async Task<(bool Success, string? Error, bool NotFound, ImpactAnalysisResponseDTO? Data)> GetImpactAnalysisAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            if (!_tenantContext.HasTenant || !_tenantContext.TenantId.HasValue)
            {
                return (false, "An authenticated tenant context is required to retrieve impact analysis.", true, null);
            }

            var currentTenantId = _tenantContext.TenantId.Value;
            var incident = await _incidentRepository.GetByIdAsync(id, cancellationToken);
            if (incident == null || incident.TenantId != currentTenantId)
            {
                return (false, $"Incident with ID '{id}' was not found.", true, null);
            }

            // Delegate downstream system & operation impact calculation to existing ImpactAnalysisService
            return await _impactAnalysisService.AnalyzeImpactAsync(incident.FailedSystemId, cancellationToken);
        }

        private static bool IsValidTransition(IncidentStatus current, IncidentStatus target)
        {
            if (current == target)
            {
                return true; // No-op transition
            }

            return current switch
            {
                IncidentStatus.Open => target is IncidentStatus.Investigating or IncidentStatus.Mitigating or IncidentStatus.Resolved or IncidentStatus.Closed,
                IncidentStatus.Investigating => target is IncidentStatus.Open or IncidentStatus.Mitigating or IncidentStatus.Resolved or IncidentStatus.Closed,
                IncidentStatus.Mitigating => target is IncidentStatus.Investigating or IncidentStatus.Resolved or IncidentStatus.Closed,
                IncidentStatus.Resolved => target is IncidentStatus.Closed or IncidentStatus.Investigating or IncidentStatus.Mitigating,
                IncidentStatus.Closed => false, // Closed is terminal
                _ => false
            };
        }

        private static IncidentResponseDTO MapToResponse(Incident incident)
        {
            return new IncidentResponseDTO
            {
                Id = incident.Id,
                TenantId = incident.TenantId,
                FailedSystemId = incident.FailedSystemId,
                FailedSystemName = incident.FailedSystem?.Name ?? string.Empty,
                FailedSystemCriticality = incident.FailedSystem?.Criticality ?? SystemCriticality.Medium,
                Title = incident.Title,
                Description = incident.Description,
                IncidentType = incident.IncidentType,
                Severity = incident.Severity,
                Status = incident.Status,
                DetectedAt = incident.DetectedAt,
                CreatedAt = incident.CreatedAt,
                UpdatedAt = incident.UpdatedAt,
                ResolvedAt = incident.ResolvedAt
            };
        }
    }
}
