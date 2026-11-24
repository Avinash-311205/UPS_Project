using Backend.Modules.Systems.DTOs;

namespace Backend.Modules.Systems.Interfaces
{
    public interface ILogisticsSystemService
    {
        Task<IEnumerable<LogisticsSystemResponseDTO>> GetAllAsync(
            bool includeInactive = false,
            CancellationToken cancellationToken = default);

        Task<LogisticsSystemResponseDTO?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        Task<(bool Success, string? Error, LogisticsSystemResponseDTO? Data)> CreateAsync(
            CreateLogisticsSystemDTO dto,
            CancellationToken cancellationToken = default);

        Task<(bool Success, string? Error, bool NotFound, LogisticsSystemResponseDTO? Data)> UpdateAsync(
            Guid id,
            UpdateLogisticsSystemDTO dto,
            CancellationToken cancellationToken = default);

        Task<(bool Success, string? Error, bool NotFound)> DeleteAsync(
            Guid id,
            CancellationToken cancellationToken = default);
    }
}
