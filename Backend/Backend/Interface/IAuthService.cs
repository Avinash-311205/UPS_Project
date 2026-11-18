using Backend.DTO;
using Backend.Model;

namespace Backend.Interface
{
    public interface IAuthService
    {
        
        Task<User?> RegisterAsync(RegisterDTO registerDto, CancellationToken cancellationToken = default);

       
        Task<(User User, string Token)?> LoginAsync(LoginDTO loginDto, CancellationToken cancellationToken = default);

        Task<(User User, string Token)?> LoginWithGoogleAsync(string credential, CancellationToken cancellationToken = default);
    }
}
