using Backend.Infrastructure.Persistence;
using Backend.Modules.Tenants.Interfaces;
using Backend.Modules.Tenants.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Modules.Tenants.Repositories
{
    public class TenantRepository : ITenantRepository
    {
        private readonly AppDbContext _context;

        public TenantRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Tenant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Tenants
                .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        }

        public async Task<Tenant?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
        {
            return await _context.Tenants
                .FirstOrDefaultAsync(t => t.Name == name, cancellationToken);
        }

        public async Task<Tenant> AddAsync(Tenant tenant, CancellationToken cancellationToken = default)
        {
            await _context.Tenants.AddAsync(tenant, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return tenant;
        }
    }
}
