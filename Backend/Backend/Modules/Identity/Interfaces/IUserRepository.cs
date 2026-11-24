using Backend.Modules.Identity.Models;

namespace Backend.Modules.Identity.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

        Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);

        Task<User> AddAsync(User user, CancellationToken cancellationToken = default);

        Task<User> UpdateAsync(User user, CancellationToken cancellationToken = default);
    }
}
