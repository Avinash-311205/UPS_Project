using Backend.Modules.Operations.DTOs;

namespace Backend.Modules.Operations.Interfaces
{
    public interface IBusinessOperationService
    {
        Task<IEnumerable<BusinessOperationResponseDTO>> GetAllAsync(
            bool includeInactive = false,
            CancellationToken cancellationToken = default);

        Task<BusinessOperationResponseDTO?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default);

        Task<(bool Success, string? Error, bool IsConflict, BusinessOperationResponseDTO? Data)> CreateAsync(
            CreateBusinessOperationDTO dto,
            CancellationToken cancellationToken = default);

        Task<(bool Success, string? Error, bool NotFound, bool IsConflict, BusinessOperationResponseDTO? Data)> UpdateAsync(
            Guid id,
            UpdateBusinessOperationDTO dto,
            CancellationToken cancellationToken = default);

        Task<(bool Success, string? Error, bool NotFound)> DeleteAsync(
            Guid id,
            CancellationToken cancellationToken = default);

        Task<(bool Success, string? Error, bool NotFound, bool IsConflict, SystemBusinessOperationResponseDTO? Data)> AssignSystemAsync(
            Guid operationId,
            Guid systemId,
            CancellationToken cancellationToken = default);

        Task<(bool Success, string? Error, bool NotFound)> RemoveSystemAsync(
            Guid operationId,
            Guid systemId,
            CancellationToken cancellationToken = default);

        Task<(bool Success, string? Error, bool NotFound, IEnumerable<SystemBusinessOperationResponseDTO>? Data)> GetSystemsForOperationAsync(
            Guid operationId,
            CancellationToken cancellationToken = default);

        Task<(bool Success, string? Error, bool NotFound, IEnumerable<SystemBusinessOperationResponseDTO>? Data)> GetOperationsForSystemAsync(
            Guid systemId,
            CancellationToken cancellationToken = default);
    }
}
