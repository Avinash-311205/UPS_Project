using Backend.Infrastructure.Persistence;
using Backend.Modules.Operations.Enums;
using Backend.Modules.Operations.Interfaces;
using Backend.Modules.Operations.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Modules.Operations.Repositories
{
    public class BusinessOperationRepository : IBusinessOperationRepository
    {
        private readonly AppDbContext _context;

        public BusinessOperationRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<BusinessOperation>> GetAllAsync(bool includeInactive = false, CancellationToken cancellationToken = default)
        {
            var query = _context.BusinessOperations
                .AsNoTracking()
                .Include(o => o.SupportingSystems)
                .AsQueryable();

            if (!includeInactive)
            {
                query = query.Where(o => o.LifecycleStatus == OperationLifecycleStatus.Active);
            }

            return await query.OrderBy(o => o.Name).ToListAsync(cancellationToken);
        }

        public async Task<BusinessOperation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.BusinessOperations
                .Include(o => o.SupportingSystems)
                    .ThenInclude(sbo => sbo.LogisticsSystem)
                .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
        }

        public async Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default)
        {
            var query = _context.BusinessOperations.AsQueryable();
            if (excludeId.HasValue)
            {
                query = query.Where(o => o.Id != excludeId.Value);
            }

            return await query.AnyAsync(o => o.Name.ToLower() == name.ToLower(), cancellationToken);
        }

        public async Task<BusinessOperation> AddAsync(BusinessOperation operation, CancellationToken cancellationToken = default)
        {
            await _context.BusinessOperations.AddAsync(operation, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return operation;
        }

        public async Task<BusinessOperation> UpdateAsync(BusinessOperation operation, CancellationToken cancellationToken = default)
        {
            _context.BusinessOperations.Update(operation);
            await _context.SaveChangesAsync(cancellationToken);
            return operation;
        }

        public async Task DeleteAsync(BusinessOperation operation, CancellationToken cancellationToken = default)
        {
            _context.BusinessOperations.Remove(operation);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<SystemBusinessOperation?> GetAssignmentAsync(Guid operationId, Guid systemId, CancellationToken cancellationToken = default)
        {
            return await _context.SystemBusinessOperations
                .Include(sbo => sbo.BusinessOperation)
                .Include(sbo => sbo.LogisticsSystem)
                .FirstOrDefaultAsync(sbo => sbo.BusinessOperationId == operationId && sbo.LogisticsSystemId == systemId, cancellationToken);
        }

        public async Task<bool> AssignmentExistsAsync(Guid operationId, Guid systemId, CancellationToken cancellationToken = default)
        {
            return await _context.SystemBusinessOperations
                .AnyAsync(sbo => sbo.BusinessOperationId == operationId && sbo.LogisticsSystemId == systemId, cancellationToken);
        }

        public async Task<SystemBusinessOperation> AddAssignmentAsync(SystemBusinessOperation assignment, CancellationToken cancellationToken = default)
        {
            await _context.SystemBusinessOperations.AddAsync(assignment, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return assignment;
        }

        public async Task RemoveAssignmentAsync(SystemBusinessOperation assignment, CancellationToken cancellationToken = default)
        {
            _context.SystemBusinessOperations.Remove(assignment);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<IEnumerable<SystemBusinessOperation>> GetSystemsByOperationIdAsync(Guid operationId, CancellationToken cancellationToken = default)
        {
            return await _context.SystemBusinessOperations
                .AsNoTracking()
                .Include(sbo => sbo.LogisticsSystem)
                .Where(sbo => sbo.BusinessOperationId == operationId)
                .OrderBy(sbo => sbo.LogisticsSystem != null ? sbo.LogisticsSystem.Name : string.Empty)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<SystemBusinessOperation>> GetOperationsBySystemIdAsync(Guid systemId, CancellationToken cancellationToken = default)
        {
            return await _context.SystemBusinessOperations
                .AsNoTracking()
                .Include(sbo => sbo.BusinessOperation)
                .Where(sbo => sbo.LogisticsSystemId == systemId)
                .OrderBy(sbo => sbo.BusinessOperation != null ? sbo.BusinessOperation.Name : string.Empty)
                .ToListAsync(cancellationToken);
        }
    }
}
