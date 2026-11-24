using Backend.Modules.Identity.DTOs;
using Backend.Modules.Identity.Models;

namespace Backend.Modules.Identity.Interfaces
{
    public interface IAuthService
    {
        Task<User?> RegisterAsync(RegisterDTO registerDto, CancellationToken cancellationToken = default);

        Task<(User User, string Token)?> LoginAsync(LoginDTO loginDto, CancellationToken cancellationToken = default);

        Task<(User User, string Token)?> LoginWithGoogleAsync(string credential, CancellationToken cancellationToken = default);
    }
}
