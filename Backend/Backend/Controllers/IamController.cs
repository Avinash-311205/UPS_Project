using System.Security.Claims;
using Backend.Common.Responses;
using Backend.Modules.IAM.DTOs;
using Backend.Modules.IAM.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/iam")]
    [Authorize(Policy = "RootOnly")]
    public class IamController : ControllerBase
    {
        private readonly IIamService _iamService;

        public IamController(IIamService iamService)
        {
            _iamService = iamService;
        }

        /// <summary>
        /// Retrieves all IAM users belonging to the authenticated Root user's tenant.
        /// </summary>
        [HttpGet("users")]
        [ProducesResponseType(typeof(ResponseDTO<IEnumerable<IamUserResponseDTO>>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAllUsers(CancellationToken cancellationToken)
        {
            var users = await _iamService.GetAllAsync(cancellationToken);
            return Ok(new ResponseDTO<IEnumerable<IamUserResponseDTO>>
            {
                Success = true,
                Message = "IAM users retrieved successfully.",
                Data = users
            });
        }

        /// <summary>
        /// Creates a new IAM user under the authenticated Root user's tenant.
        /// </summary>
        [HttpPost("users")]
        [ProducesResponseType(typeof(ResponseDTO<IamUserResponseDTO>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ResponseDTO<IamUserResponseDTO>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ResponseDTO<IamUserResponseDTO>), StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CreateUser([FromBody] CreateIamUserDTO dto, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ResponseDTO<IamUserResponseDTO>
                {
                    Success = false,
                    Message = "Validation failed.",
                    Data = null
                });
            }

            try
            {
                var createdUser = await _iamService.CreateAsync(dto, cancellationToken);
                return StatusCode(StatusCodes.Status201Created, new ResponseDTO<IamUserResponseDTO>
                {
                    Success = true,
                    Message = "IAM user created successfully.",
                    Data = createdUser
                });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new ResponseDTO<IamUserResponseDTO>
                {
                    Success = false,
                    Message = ex.Message,
                    Data = null
                });
            }
        }

        /// <summary>
        /// Updates the status (Active/Inactive) of an IAM user within the authenticated Root user's tenant.
        /// </summary>
        [HttpPatch("users/{id:int}/status")]
        [ProducesResponseType(typeof(ResponseDTO<IamUserResponseDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseDTO<IamUserResponseDTO>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ResponseDTO<IamUserResponseDTO>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpdateUserStatus(
            int id,
            [FromBody] UpdateIamUserStatusDTO dto,
            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ResponseDTO<IamUserResponseDTO>
                {
                    Success = false,
                    Message = "Validation failed.",
                    Data = null
                });
            }

            var callerUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                                   ?? User.FindFirst("sub")?.Value;
            int.TryParse(callerUserIdClaim, out var callerUserId);

            try
            {
                var updatedUser = await _iamService.UpdateStatusAsync(id, dto, callerUserId, cancellationToken);
                if (updatedUser == null)
                {
                    return NotFound(new ResponseDTO<IamUserResponseDTO>
                    {
                        Success = false,
                        Message = $"User with ID '{id}' was not found in your organization.",
                        Data = null
                    });
                }

                return Ok(new ResponseDTO<IamUserResponseDTO>
                {
                    Success = true,
                    Message = $"IAM user status updated to {dto.Status}.",
                    Data = updatedUser
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ResponseDTO<IamUserResponseDTO>
                {
                    Success = false,
                    Message = ex.Message,
                    Data = null
                });
            }
        }

        /// <summary>
        /// Resets the password for an IAM user within the authenticated Root user's tenant.
        /// </summary>
        [HttpPost("users/{id:int}/reset-password")]
        [ProducesResponseType(typeof(ResponseDTO<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseDTO<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ResponseDTO<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> ResetUserPassword(
            int id,
            [FromBody] ResetIamUserPasswordDTO dto,
            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ResponseDTO<object>
                {
                    Success = false,
                    Message = "Validation failed.",
                    Data = null
                });
            }

            try
            {
                var success = await _iamService.ResetPasswordAsync(id, dto, cancellationToken);
                if (!success)
                {
                    return NotFound(new ResponseDTO<object>
                    {
                        Success = false,
                        Message = $"User with ID '{id}' was not found in your organization.",
                        Data = null
                    });
                }

                return Ok(new ResponseDTO<object>
                {
                    Success = true,
                    Message = "IAM user password has been reset successfully. Existing sessions have been invalidated.",
                    Data = null
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ResponseDTO<object>
                {
                    Success = false,
                    Message = ex.Message,
                    Data = null
                });
            }
        }
    }
}
