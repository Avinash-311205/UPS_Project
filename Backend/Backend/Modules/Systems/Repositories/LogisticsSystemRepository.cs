using Backend.Infrastructure.Persistence;
using Backend.Modules.Systems.Interfaces;
using Backend.Modules.Systems.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Modules.Systems.Repositories
{
    public class LogisticsSystemRepository : ILogisticsSystemRepository
    {
        private readonly AppDbContext _context;

        public LogisticsSystemRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<LogisticsSystem>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Systems
                .AsNoTracking()
                .OrderBy(s => s.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task<LogisticsSystem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Systems
                .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        }

        public async Task<LogisticsSystem> AddAsync(LogisticsSystem system, CancellationToken cancellationToken = default)
        {
            await _context.Systems.AddAsync(system, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return system;
        }

        public async Task<LogisticsSystem> UpdateAsync(LogisticsSystem system, CancellationToken cancellationToken = default)
        {
            _context.Systems.Update(system);
            await _context.SaveChangesAsync(cancellationToken);
            return system;
        }

        public async Task DeleteAsync(LogisticsSystem system, CancellationToken cancellationToken = default)
        {
            _context.Systems.Remove(system);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
