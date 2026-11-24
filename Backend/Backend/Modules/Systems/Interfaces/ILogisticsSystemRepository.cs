using Backend.Modules.Systems.Models;

namespace Backend.Modules.Systems.Interfaces
{
    public interface ILogisticsSystemRepository
    {
        Task<IEnumerable<LogisticsSystem>> GetAllAsync(CancellationToken cancellationToken = default);

        Task<LogisticsSystem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        Task<LogisticsSystem> AddAsync(LogisticsSystem system, CancellationToken cancellationToken = default);

        Task<LogisticsSystem> UpdateAsync(LogisticsSystem system, CancellationToken cancellationToken = default);

        Task DeleteAsync(LogisticsSystem system, CancellationToken cancellationToken = default);
    }
}
