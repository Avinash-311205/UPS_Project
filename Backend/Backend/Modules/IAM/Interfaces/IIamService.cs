using Backend.Modules.IAM.DTOs;

namespace Backend.Modules.IAM.Interfaces
{
    public interface IIamService
    {
        Task<IEnumerable<IamUserResponseDTO>> GetAllAsync(CancellationToken cancellationToken = default);

        Task<IamUserResponseDTO> CreateAsync(CreateIamUserDTO dto, CancellationToken cancellationToken = default);

        Task<IamUserResponseDTO?> UpdateStatusAsync(int id, UpdateIamUserStatusDTO dto, int callerUserId, CancellationToken cancellationToken = default);

        Task<bool> ResetPasswordAsync(int id, ResetIamUserPasswordDTO dto, CancellationToken cancellationToken = default);
    }
}
