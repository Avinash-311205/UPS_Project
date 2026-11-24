using Backend.Modules.Dependencies.Models;

namespace Backend.Modules.Dependencies.Interfaces
{
    public interface ISystemDependencyRepository
    {
        Task<IEnumerable<SystemDependency>> GetAllAsync(CancellationToken cancellationToken = default);

        Task<SystemDependency?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        Task<bool> ExistsAsync(Guid dependentSystemId, Guid dependencySystemId, CancellationToken cancellationToken = default);

        Task<SystemDependency> AddAsync(SystemDependency dependency, CancellationToken cancellationToken = default);

        Task<SystemDependency> UpdateAsync(SystemDependency dependency, CancellationToken cancellationToken = default);

        Task DeleteAsync(SystemDependency dependency, CancellationToken cancellationToken = default);
    }
}
