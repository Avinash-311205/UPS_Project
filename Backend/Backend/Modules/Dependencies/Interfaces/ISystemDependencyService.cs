using Backend.Modules.Dependencies.DTOs;

namespace Backend.Modules.Dependencies.Interfaces
{
    public interface ISystemDependencyService
    {
        Task<IEnumerable<SystemDependencyResponseDTO>> GetAllAsync(CancellationToken cancellationToken = default);

        Task<SystemDependencyResponseDTO?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        Task<(bool Success, string? Error, SystemDependencyResponseDTO? Data)> CreateAsync(
            CreateSystemDependencyDTO dto,
            CancellationToken cancellationToken = default);

        Task<(bool Success, string? Error, bool NotFound, SystemDependencyResponseDTO? Data)> UpdateAsync(
            Guid id,
            UpdateSystemDependencyDTO dto,
            CancellationToken cancellationToken = default);

        Task<(bool Success, string? Error, bool NotFound)> DeleteAsync(
            Guid id,
            CancellationToken cancellationToken = default);
    }
}
