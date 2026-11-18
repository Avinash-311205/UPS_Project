using Backend.DTO;
using Backend.Interface;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthenticationController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthenticationController(IAuthService authService)
        {
            _authService = authService;
        }

        /// <summary>
        /// Registers a new user.
        /// </summary>
        /// <param name="registerDto">The user registration details.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>ApiResponse with the created user data or error message.</returns>
        [HttpPost("register")]
        [ProducesResponseType(typeof(ResponseDTO<UserResponseDTO>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ResponseDTO<UserResponseDTO>), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Register([FromBody] RegisterDTO registerDto, CancellationToken cancellationToken)
        {
            var user = await _authService.RegisterAsync(registerDto, cancellationToken);

            if (user == null)
            {
                return Conflict(new ResponseDTO<UserResponseDTO>
                {
                    Success = false,
                    Message = "A user with this email already exists.",
                    Data = null
                });
            }

            var userResponse = new UserResponseDTO
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email
            };

            return StatusCode(StatusCodes.Status201Created, new ResponseDTO<UserResponseDTO>
            {
                Success = true,
                Message = "User registered successfully.",
                Data = userResponse
            });
        }

        /// <summary>
        /// Authenticates a user and returns a token response.
        /// </summary>
        /// <param name="loginDto">The login credentials.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>ApiResponse with login response data or error message.</returns>
        [HttpPost("login")]
        [ProducesResponseType(typeof(ResponseDTO<LoginResponseDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseDTO<LoginResponseDTO>), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] LoginDTO loginDto, CancellationToken cancellationToken)
        {
            var result = await _authService.LoginAsync(loginDto, cancellationToken);

            if (result == null)
            {
                return Unauthorized(new ResponseDTO<LoginResponseDTO>
                {
                    Success = false,
                    Message = "Invalid email or password.",
                    Data = null
                });
            }

            var loginResponse = new LoginResponseDTO
            {
                Token = result.Value.Token,
                Username = result.Value.User.Username,
                Email = result.Value.User.Email
            };

            return Ok(new ResponseDTO<LoginResponseDTO>
            {
                Success = true,
                Message = "Login successful.",
                Data = loginResponse
            });
        }

        [HttpPost("google")]
        [ProducesResponseType(typeof(ResponseDTO<LoginResponseDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseDTO<LoginResponseDTO>), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginDTO googleLoginDto, CancellationToken cancellationToken)
        {
            var result = await _authService.LoginWithGoogleAsync(googleLoginDto.Credential, cancellationToken);
            if (result == null)
            {
                return Unauthorized(new ResponseDTO<LoginResponseDTO>
                {
                    Success = false,
                    Message = "Google sign-in could not be verified.",
                    Data = null
                });
            }

            return Ok(new ResponseDTO<LoginResponseDTO>
            {
                Success = true,
                Message = "Login successful.",
                Data = new LoginResponseDTO
                {
                    Token = result.Value.Token,
                    Username = result.Value.User.Username,
                    Email = result.Value.User.Email
                }
            });
        }
    }
}
