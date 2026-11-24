using Backend.Modules.Operations.Models;

namespace Backend.Modules.Operations.Interfaces
{
    public interface IBusinessOperationRepository
    {
        Task<IEnumerable<BusinessOperation>> GetAllAsync(bool includeInactive = false, CancellationToken cancellationToken = default);

        Task<BusinessOperation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default);

        Task<BusinessOperation> AddAsync(BusinessOperation operation, CancellationToken cancellationToken = default);

        Task<BusinessOperation> UpdateAsync(BusinessOperation operation, CancellationToken cancellationToken = default);

        Task DeleteAsync(BusinessOperation operation, CancellationToken cancellationToken = default);

        // System - Operation relationship
        Task<SystemBusinessOperation?> GetAssignmentAsync(Guid operationId, Guid systemId, CancellationToken cancellationToken = default);

        Task<bool> AssignmentExistsAsync(Guid operationId, Guid systemId, CancellationToken cancellationToken = default);

        Task<SystemBusinessOperation> AddAssignmentAsync(SystemBusinessOperation assignment, CancellationToken cancellationToken = default);

        Task RemoveAssignmentAsync(SystemBusinessOperation assignment, CancellationToken cancellationToken = default);

        Task<IEnumerable<SystemBusinessOperation>> GetSystemsByOperationIdAsync(Guid operationId, CancellationToken cancellationToken = default);

        Task<IEnumerable<SystemBusinessOperation>> GetOperationsBySystemIdAsync(Guid systemId, CancellationToken cancellationToken = default);
    }
}
