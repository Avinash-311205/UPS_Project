using Backend.Common.Responses;
using Backend.Modules.Dependencies.DTOs;
using Backend.Modules.Dependencies.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/system-dependencies")]
    [Authorize]
    public class SystemDependenciesController : ControllerBase
    {
        private readonly ISystemDependencyService _dependencyService;

        public SystemDependenciesController(ISystemDependencyService dependencyService)
        {
            _dependencyService = dependencyService;
        }

        /// <summary>
        /// Retrieves all system dependencies belonging to the authenticated tenant.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ResponseDTO<IEnumerable<SystemDependencyResponseDTO>>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken = default)
        {
            var dependencies = await _dependencyService.GetAllAsync(cancellationToken);
            return Ok(new ResponseDTO<IEnumerable<SystemDependencyResponseDTO>>
            {
                Success = true,
                Message = "System dependencies retrieved successfully.",
                Data = dependencies
            });
        }

        /// <summary>
        /// Retrieves a specific system dependency by ID within the authenticated tenant.
        /// </summary>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(ResponseDTO<SystemDependencyResponseDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseDTO<SystemDependencyResponseDTO>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken = default)
        {
            var dependency = await _dependencyService.GetByIdAsync(id, cancellationToken);
            if (dependency == null)
            {
                return NotFound(new ResponseDTO<SystemDependencyResponseDTO>
                {
                    Success = false,
                    Message = $"System dependency with ID '{id}' was not found.",
                    Data = null
                });
            }

            return Ok(new ResponseDTO<SystemDependencyResponseDTO>
            {
                Success = true,
                Message = "System dependency retrieved successfully.",
                Data = dependency
            });
        }

        /// <summary>
        /// Registers a new system dependency relationship between two active systems within the authenticated tenant.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ResponseDTO<SystemDependencyResponseDTO>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ResponseDTO<SystemDependencyResponseDTO>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Create(
            [FromBody] CreateSystemDependencyDTO createDto,
            CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ResponseDTO<SystemDependencyResponseDTO>
                {
                    Success = false,
                    Message = "Validation failed.",
                    Data = null
                });
            }

            var (success, error, data) = await _dependencyService.CreateAsync(createDto, cancellationToken);
            if (!success)
            {
                return BadRequest(new ResponseDTO<SystemDependencyResponseDTO>
                {
                    Success = false,
                    Message = error ?? "Failed to create system dependency.",
                    Data = null
                });
            }

            return StatusCode(StatusCodes.Status201Created, new ResponseDTO<SystemDependencyResponseDTO>
            {
                Success = true,
                Message = "System dependency created successfully.",
                Data = data
            });
        }

        /// <summary>
        /// Updates the dependency type of an existing system dependency within the authenticated tenant.
        /// </summary>
        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(ResponseDTO<SystemDependencyResponseDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseDTO<SystemDependencyResponseDTO>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ResponseDTO<SystemDependencyResponseDTO>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Update(
            Guid id,
            [FromBody] UpdateSystemDependencyDTO updateDto,
            CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ResponseDTO<SystemDependencyResponseDTO>
                {
                    Success = false,
                    Message = "Validation failed.",
                    Data = null
                });
            }

            var (success, error, notFound, data) = await _dependencyService.UpdateAsync(id, updateDto, cancellationToken);
            if (!success)
            {
                if (notFound)
                {
                    return NotFound(new ResponseDTO<SystemDependencyResponseDTO>
                    {
                        Success = false,
                        Message = error ?? $"System dependency with ID '{id}' was not found.",
                        Data = null
                    });
                }

                return BadRequest(new ResponseDTO<SystemDependencyResponseDTO>
                {
                    Success = false,
                    Message = error ?? "Failed to update system dependency.",
                    Data = null
                });
            }

            return Ok(new ResponseDTO<SystemDependencyResponseDTO>
            {
                Success = true,
                Message = "System dependency updated successfully.",
                Data = data
            });
        }

        /// <summary>
        /// Deletes an existing system dependency relationship. Does not delete either referenced system.
        /// </summary>
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(typeof(ResponseDTO<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseDTO<bool>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
        {
            var (success, error, notFound) = await _dependencyService.DeleteAsync(id, cancellationToken);
            if (!success)
            {
                if (notFound)
                {
                    return NotFound(new ResponseDTO<bool>
                    {
                        Success = false,
                        Message = error ?? $"System dependency with ID '{id}' was not found.",
                        Data = false
                    });
                }

                return BadRequest(new ResponseDTO<bool>
                {
                    Success = false,
                    Message = error ?? "Failed to delete system dependency.",
                    Data = false
                });
            }

            return Ok(new ResponseDTO<bool>
            {
                Success = true,
                Message = "System dependency deleted successfully.",
                Data = true
            });
        }
    }
}
