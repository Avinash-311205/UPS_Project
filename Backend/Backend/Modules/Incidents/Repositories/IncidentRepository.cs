using Backend.Infrastructure.Persistence;
using Backend.Modules.Incidents.Enums;
using Backend.Modules.Incidents.Interfaces;
using Backend.Modules.Incidents.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Modules.Incidents.Repositories
{
    public class IncidentRepository : IIncidentRepository
    {
        private readonly AppDbContext _context;

        public IncidentRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Incident>> GetAllAsync(
            IncidentStatus? status = null,
            IncidentSeverity? severity = null,
            IncidentType? type = null,
            Guid? failedSystemId = null,
            CancellationToken cancellationToken = default)
        {
            var query = _context.Incidents
                .AsNoTracking()
                .Include(i => i.FailedSystem)
                .AsQueryable();

            if (status.HasValue)
            {
                query = query.Where(i => i.Status == status.Value);
            }

            if (severity.HasValue)
            {
                query = query.Where(i => i.Severity == severity.Value);
            }

            if (type.HasValue)
            {
                query = query.Where(i => i.IncidentType == type.Value);
            }

            if (failedSystemId.HasValue)
            {
                query = query.Where(i => i.FailedSystemId == failedSystemId.Value);
            }

            return await query
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<Incident?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Incidents
                .Include(i => i.FailedSystem)
                .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
        }

        public async Task<Incident> AddAsync(Incident incident, CancellationToken cancellationToken = default)
        {
            await _context.Incidents.AddAsync(incident, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return incident;
        }

        public async Task<Incident> UpdateAsync(Incident incident, CancellationToken cancellationToken = default)
        {
            _context.Incidents.Update(incident);
            await _context.SaveChangesAsync(cancellationToken);
            return incident;
        }

        public async Task DeleteAsync(Incident incident, CancellationToken cancellationToken = default)
        {
            _context.Incidents.Remove(incident);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
