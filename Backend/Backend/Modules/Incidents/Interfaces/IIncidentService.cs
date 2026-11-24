using Backend.Modules.ImpactAnalysis.DTOs;
using Backend.Modules.Incidents.DTOs;
using Backend.Modules.Incidents.Enums;

namespace Backend.Modules.Incidents.Interfaces
{
    public interface IIncidentService
    {
        Task<IEnumerable<IncidentResponseDTO>> GetAllAsync(
            IncidentStatus? status = null,
            IncidentSeverity? severity = null,
            IncidentType? type = null,
            Guid? failedSystemId = null,
            CancellationToken cancellationToken = default);

        Task<IncidentResponseDTO?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        Task<(bool Success, string? Error, bool NotFound, IncidentResponseDTO? Data)> CreateAsync(
            CreateIncidentDTO dto,
            CancellationToken cancellationToken = default);

        Task<(bool Success, string? Error, bool NotFound, IncidentResponseDTO? Data)> UpdateAsync(
            Guid id,
            UpdateIncidentDTO dto,
            CancellationToken cancellationToken = default);

        Task<(bool Success, string? Error, bool NotFound, IncidentResponseDTO? Data)> UpdateStatusAsync(
            Guid id,
            UpdateIncidentStatusDTO dto,
            CancellationToken cancellationToken = default);

        Task<(bool Success, string? Error, bool NotFound, ImpactAnalysisResponseDTO? Data)> GetImpactAnalysisAsync(
            Guid id,
            CancellationToken cancellationToken = default);
    }
}
