using Backend.Modules.Tenants.Models;

namespace Backend.Modules.Tenants.Interfaces
{
    public interface ITenantRepository
    {
        Task<Tenant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        Task<Tenant?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

        Task<Tenant> AddAsync(Tenant tenant, CancellationToken cancellationToken = default);
    }
}
