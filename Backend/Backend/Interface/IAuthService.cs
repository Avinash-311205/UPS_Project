using Backend.DTO;
using Backend.Model;

namespace Backend.Interface
{
    public interface IAuthService
    {
        
        Task<User?> RegisterAsync(RegisterDTO registerDto, CancellationToken cancellationToken = default);

       
        Task<string?> LoginAsync(LoginDTO loginDto, CancellationToken cancellationToken = default);
    }
}
