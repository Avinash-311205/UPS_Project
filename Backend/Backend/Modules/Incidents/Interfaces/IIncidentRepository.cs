using Backend.Modules.Incidents.Enums;
using Backend.Modules.Incidents.Models;

namespace Backend.Modules.Incidents.Interfaces
{
    public interface IIncidentRepository
    {
        Task<IEnumerable<Incident>> GetAllAsync(
            IncidentStatus? status = null,
            IncidentSeverity? severity = null,
            IncidentType? type = null,
            Guid? failedSystemId = null,
            CancellationToken cancellationToken = default);

        Task<Incident?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        Task<Incident> AddAsync(Incident incident, CancellationToken cancellationToken = default);

        Task<Incident> UpdateAsync(Incident incident, CancellationToken cancellationToken = default);

        Task DeleteAsync(Incident incident, CancellationToken cancellationToken = default);
    }
}
