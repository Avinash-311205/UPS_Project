using Backend.Infrastructure.Persistence;
using Backend.Modules.Dependencies.Interfaces;
using Backend.Modules.Dependencies.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Modules.Dependencies.Repositories
{
    public class SystemDependencyRepository : ISystemDependencyRepository
    {
        private readonly AppDbContext _context;

        public SystemDependencyRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<SystemDependency>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Dependencies
                .AsNoTracking()
                .Include(d => d.DependentSystem)
                .Include(d => d.DependencySystem)
                .OrderBy(d => d.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<SystemDependency?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Dependencies
                .Include(d => d.DependentSystem)
                .Include(d => d.DependencySystem)
                .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        }

        public async Task<bool> ExistsAsync(Guid dependentSystemId, Guid dependencySystemId, CancellationToken cancellationToken = default)
        {
            return await _context.Dependencies
                .AnyAsync(d => d.DependentSystemId == dependentSystemId && d.DependencySystemId == dependencySystemId, cancellationToken);
        }

        public async Task<SystemDependency> AddAsync(SystemDependency dependency, CancellationToken cancellationToken = default)
        {
            await _context.Dependencies.AddAsync(dependency, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return dependency;
        }

        public async Task<SystemDependency> UpdateAsync(SystemDependency dependency, CancellationToken cancellationToken = default)
        {
            _context.Dependencies.Update(dependency);
            await _context.SaveChangesAsync(cancellationToken);
            return dependency;
        }

        public async Task DeleteAsync(SystemDependency dependency, CancellationToken cancellationToken = default)
        {
            _context.Dependencies.Remove(dependency);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
